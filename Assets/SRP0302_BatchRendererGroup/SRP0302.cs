using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0302 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0302", priority = 1)]
    static void CreateSRP0302()
    {
        var instance = ScriptableObject.CreateInstance<SRP0302>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0302.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0302Instance();
    }
}

public class SRP0302Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0302_Pass"); //The shader pass tag just for SRP0302

    public SRP0302Instance()
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
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //SRPBatcher
            GraphicsSettings.useScriptableRenderPipelineBatching = true;

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

            context.Submit();

            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}