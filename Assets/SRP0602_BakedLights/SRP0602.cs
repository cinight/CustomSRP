using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0602 : RenderPipelineAsset<SRP0602Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0602", priority = 1)]
    static void CreateSRP0602()
    {
        var instance = ScriptableObject.CreateInstance<SRP0602>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0602.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0602Instance();
    }
}

public class SRP0602Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0602_Pass"); //The shader pass tag just for SRP0602

    public SRP0602Instance()
    {
        #if UNITY_EDITOR
        SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
        {           
            //Lighting Settings - Mixed Lighting - default
            defaultMixedLightingModes =
            SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
            //Lighting Settings - Mixed Lighting - supported
            mixedLightingModes =
            SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive |
            SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly |
            SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,

            //Lighting Settings - Lightmapping Settings - supported
            lightmapsModes = LightmapsMode.NonDirectional,
            //LightmapsMode.CombinedDirectional |

            //Lighting Settings - Other Settings - Fog
            overridesFog = true,

            //Lighting Settings - Other Settings
            overridesOtherLightingSettings = true,

            //Lighting Settings - Environment
            overridesEnvironmentLighting = false,
            
            //Light Component - Mode - supported
            lightmapBakeTypes =
            LightmapBakeType.Baked |
            LightmapBakeType.Mixed,// |
            //LightmapBakeType.Realtime
            
            //MeshRenderer component
            motionVectors = false,
            receiveShadows = true,
            lightProbeProxyVolumes = true,

            //ReflectionProbe component
            reflectionProbes = true,
            reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None, //ReflectionProbeModes.Rotation

            //Material
            editableMaterialRenderQueue = true,

            //
            rendererPriority = false
        };
        
        #endif
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context,camera);

            //Culling
            ScriptableCullingParameters cullingParams;
            if (!camera.TryGetCullingParameters(out cullingParams))
                continue;
            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings)
            {
                perObjectData = PerObjectData.Lightmaps | 
                                PerObjectData.LightProbe | 
                                PerObjectData.LightProbeProxyVolume |
                                PerObjectData.ReflectionProbes
            };
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);
            GraphicsSettings.useScriptableRenderPipelineBatching = false; 
            // ^if it's true it breaks the baked data

            //Skybox
            if(drawSkyBox)
            {
                CustomSRPUtil.RenderSkybox(context, camera);
            }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Opaque Objects", context, cull, filterSettings, drawSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}