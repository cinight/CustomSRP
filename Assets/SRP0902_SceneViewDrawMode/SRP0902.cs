using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0902 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0902", priority = 1)]
    static void CreateSRP0902()
    {
        var instance = ScriptableObject.CreateInstance<SRP0902>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0902.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0902Instance();
    }
}

public class SRP0902Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0902_Pass"); //The shader pass tag just for SRP0902

    public SRP0902Instance()
    {
        #if UNITY_EDITOR
            SRP0902SceneView.SetupDrawMode();
        #endif
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(camera);

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
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //SceneViewDrawMode
            #if UNITY_EDITOR
               bool isSceneViewCam = camera.cameraType == CameraType.SceneView;
                if(isSceneViewCam) 
                {
                    Material debugMaterial = CustomDrawModeAssetObject.GetDrawModeMaterial();
                    if (debugMaterial != null)
                    {
                        sortingSettings.criteria = SortingCriteria.None;
                        filterSettings.renderQueueRange = RenderQueueRange.all;
                        DrawingSettings debugSettings = new DrawingSettings(new ShaderTagId("debugMaterial"), sortingSettings)
                        {
                            perObjectData = PerObjectData.None,
                            overrideMaterial = debugMaterial,
                            overrideMaterialPassIndex = 0
                        };
                        debugSettings.SetShaderPassName(1, m_PassName);
                        context.DrawRenderers(cull, ref debugSettings, ref filterSettings);
                        context.Submit();
                        continue;
                    }
                }
            #endif

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            context.Submit();
        }
    }
}