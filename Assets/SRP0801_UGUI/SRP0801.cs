using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0801 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0801", priority = 1)]
    static void CreateSRP0801()
    {
        var instance = ScriptableObject.CreateInstance<SRP0801>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0801.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0801Instance();
    }
}

public class SRP0801Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0801_Pass"); //The shader pass tag just for SRP0801
    private static readonly ShaderTagId m_PassNameDefault = new ShaderTagId("SRPDefaultUnlit"); //The shader pass tag for replacing shaders without pass

    public SRP0801Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        foreach (Camera camera in cameras)
        {
            #if UNITY_EDITOR
            bool isSceneViewCam = camera.cameraType == CameraType.SceneView;
            if(isSceneViewCam) ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); //This makes the UI Canvas geometry appear on scene view
            #endif

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
           DrawingSettings drawSettingsDefault = new DrawingSettings(m_PassNameDefault, sortingSettings);
            //This will let you draw shader passes without the LightMode,
            //thus it draws the default UGUI materials
            drawSettingsDefault.SetShaderPassName(1,m_PassNameDefault); 
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  CustomSRPUtil.RenderSkybox(context, camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Opaque Objects", context, cull, filterSettings, drawSettings);

            //Opaque default
            CustomSRPUtil.RenderObjects("Render Opaque Objects Default Pass", context, cull, filterSettings, drawSettingsDefault);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);

            //Transparent default
            CustomSRPUtil.RenderObjects("Render Transparent Objects Default Pass", context, cull, filterSettings, drawSettingsDefault);

            //SceneView fix, so that it draws the gizmos on scene view
            #if UNITY_EDITOR
            if (isSceneViewCam)
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            #endif

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}