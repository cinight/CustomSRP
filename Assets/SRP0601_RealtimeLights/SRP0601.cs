using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[ExecuteInEditMode]
public class SRP0601 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0601", priority = 1)]
    static void CreateSRP0601()
    {
        var instance = ScriptableObject.CreateInstance<SRP0601>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0601.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0601Instance();
    }
}

public class SRP0601Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0601_Pass"); //The shader pass tag just for SRP0601

    //Realtime Lights
    static int lightColorID = Shader.PropertyToID("_LightColorArray");
    static int lightDataID = Shader.PropertyToID("_LightDataArray");
    static int lightSpotDirID = Shader.PropertyToID("_LightSpotDirArray");
    private const int lightCount = 16;      
    Vector4[] lightColor = new Vector4[lightCount];
    Vector4[] lightData = new Vector4[lightCount];
    Vector4[] lightSpotDir = new Vector4[lightCount];

    public SRP0601Instance()
    {
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

            //SetUp Lighting variables
            SetUpRealtimeLightingVariables(context,cull);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Camera clear flag
            var cmd = CommandBufferPool.Get("Clear");
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings)
            {
                perObjectData = PerObjectData.LightIndices | PerObjectData.LightData
            };
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            context.Submit();
        }
    }

    private void SetUpRealtimeLightingVariables(ScriptableRenderContext context, CullingResults cull)
    {
        for (var i = 0; i < lightCount; i++)
        {
            lightColor[i] = Vector4.zero;
            lightData[i] = Vector4.zero;
            lightSpotDir[i] = Vector4.zero;

            if( i >= cull.visibleLights.Length ) continue;
            VisibleLight light = cull.visibleLights[i];
            
            if (light.lightType == LightType.Directional)
            {
                lightData[i] = light.localToWorldMatrix.MultiplyVector(Vector3.back);
                lightColor[i] = light.finalColor;
                lightColor[i].w = -1; //for identifying it is a directional light in shader
                
            }
            else if (light.lightType == LightType.Point)
            {
                lightData[i] = light.localToWorldMatrix.GetColumn(3);
                lightData[i].w = light.range;
                lightColor[i] = light.finalColor;
                lightColor[i].w = -2; //for identifying it is a point light in shader
            }
            else if (light.lightType == LightType.Spot)
            {
                lightData[i] = light.localToWorldMatrix.GetColumn(3);
                lightData[i].w = 1f /Mathf.Max(light.range * light.range, 0.00001f);

                lightSpotDir[i] = light.localToWorldMatrix.GetColumn(2);
                lightSpotDir[i].x = -lightSpotDir[i].x;
                lightSpotDir[i].y = -lightSpotDir[i].y;
                lightSpotDir[i].z = -lightSpotDir[i].z;
                lightColor[i] = light.finalColor;

                float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
				float outerCos = Mathf.Cos(outerRad);
                float outerTan  = Mathf.Tan(outerRad);
				float innerCos = Mathf.Cos(Mathf.Atan(((46f / 64f) * outerTan)));
                float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);

                //Spotlight attenuation
                lightSpotDir[i].w = 1f / angleRange;
				lightColor[i].w = -outerCos * lightSpotDir[i].w;
            }
            else
            {
                // If it's not a point / directional / spot light, we ignore the light.
                continue;
            }
        }
        
        CommandBuffer cmdLight = CommandBufferPool.Get("Set-up Light Buffer");
        cmdLight.SetGlobalVectorArray(lightDataID, lightData);
        cmdLight.SetGlobalVectorArray(lightColorID, lightColor);
        cmdLight.SetGlobalVectorArray(lightSpotDirID, lightSpotDir);
        context.ExecuteCommandBuffer(cmdLight);
        CommandBufferPool.Release(cmdLight);
    }
}