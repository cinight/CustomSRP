using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0901 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0901", priority = 1)]
    static void CreateSRP0901()
    {
        var instance = ScriptableObject.CreateInstance<SRP0901>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0901.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0901Instance();
    }
}

public class SRP0901Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0901_Pass"); //The shader pass tag just for SRP0901

    public SRP0901Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(cameras);

        foreach (Camera camera in cameras)
        {
            #if UNITY_EDITOR
            bool isSceneViewCam = camera.cameraType == CameraType.SceneView;
            if(isSceneViewCam) ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); //This makes the UI Canvas geometry appear on scene view
            #endif

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

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" ClearFlag";
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

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

            //SceneView fix, so that it draws the gizmos on scene view
            #if UNITY_EDITOR
            if (isSceneViewCam)
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            #endif

            context.Submit();
        }
    }
}