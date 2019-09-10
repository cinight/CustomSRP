using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0405 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0405", priority = 1)]
    static void CreateSRP0405()
    {
        var instance = ScriptableObject.CreateInstance<SRP0405>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0405.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0405Instance();
    }
}

public class SRP0405Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0405_Pass"); //The shader pass tag just for SRP0405

    //Custom callbacks
    public static event Action<Camera,ScriptableRenderContext> afterSkybox;
    public static event Action<Camera,ScriptableRenderContext> afterOpaqueObject;
    public static event Action<Camera,ScriptableRenderContext> afterTransparentObject;

    public SRP0405Instance()
    {
    }

    public static void AfterSkybox(Camera camera, ScriptableRenderContext context)
    {
        afterSkybox?.Invoke(camera,context);
    }

    public static void AfterOpaqueObject(Camera camera, ScriptableRenderContext context)
    {
        afterOpaqueObject?.Invoke(camera,context);
    }

    public static void AfterTransparentObject(Camera camera, ScriptableRenderContext context)
    {
        afterTransparentObject?.Invoke(camera,context);
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

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Callback
            AfterSkybox(camera,context);

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Callback
            AfterOpaqueObject(camera,context);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Callback
            AfterTransparentObject(camera,context);

            context.Submit();

            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}