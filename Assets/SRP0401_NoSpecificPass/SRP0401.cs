using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0401 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0401", priority = 1)]
    static void CreateSRP0401()
    {
        var instance = ScriptableObject.CreateInstance<SRP0401>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0401.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0401Instance();
    }
}

public class SRP0401Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0401_Pass"); //The shader pass tag just for SRP0401
    //private static readonly ShaderTagId m_PassNameEmpty = new ShaderTagId(""); //This for regonising shader passes without tag
    private static readonly ShaderTagId m_PassNameDefault = new ShaderTagId("SRPDefaultUnlit"); //The shader pass tag for replacing shaders without pass

    public SRP0401Instance()
    {
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
            DrawingSettings drawSettingsDefault = new DrawingSettings(m_PassNameDefault, sortingSettings);
                            drawSettingsDefault.SetShaderPassName(1,m_PassNameDefault); //This will let you draw shader passes without the LightMode
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Opaque default
            context.DrawRenderers(cull, ref drawSettingsDefault, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent default
            context.DrawRenderers(cull, ref drawSettingsDefault, ref filterSettings);

            context.Submit();
        }
    }
}