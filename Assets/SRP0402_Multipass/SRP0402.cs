using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0402 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0402", priority = 1)]
    static void CreateSRP0402()
    {
        var instance = ScriptableObject.CreateInstance<SRP0402>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0402.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0402Instance();
    }
}

public class SRP0402Instance : RenderPipeline
{
    //private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0402_Pass"); //The shader pass tag just for SRP0402

    public SRP0402Instance()
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
            DrawingSettings drawSettings = new DrawingSettings(new ShaderTagId("SRP0402_Pass"), sortingSettings);
            for(int i=0; i<10; i++) //MultiPass
            {
                drawSettings.SetShaderPassName(i,new ShaderTagId("SRP0402_Pass"+i));
            }
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

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