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

    //SceneView requires proper depth texture
    private static Material m_CopyDepthMaterial; //For blitting the depth buffer
    private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture");
    private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);

    public SRP0901Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(cameras);

        //SetUp materials
        if (m_CopyDepthMaterial == null) m_CopyDepthMaterial = new Material ( Shader.Find ( "Hidden/CustomSRP/SRP0901/CopyDepth"));

        foreach (Camera camera in cameras)
        {
            #if UNITY_EDITOR
            bool isSceneViewCam = camera.cameraType == CameraType.SceneView;
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

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" ClearFlag";
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Setup RenderTexture for the Depth
            #if UNITY_EDITOR
            if(isSceneViewCam)
            {
                RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                depthRTDesc.colorFormat = RenderTextureFormat.Depth;
                depthRTDesc.depthBufferBits = 32;

                CommandBuffer cmdTempId = new CommandBuffer();
                cmdTempId.GetTemporaryRT(m_DepthRTid, depthRTDesc,FilterMode.Bilinear);
                cmdTempId.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,m_DepthRT);
                cmdTempId.ClearRenderTarget(true, true, Color.black);
                context.ExecuteCommandBuffer(cmdTempId);
                cmdTempId.Release();
            }
            #endif

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //SceneView fix, so that it draws the gizmos
            #if UNITY_EDITOR
            if (isSceneViewCam)
            {
                //Provide depth to sceneview camera
                CommandBuffer cmdSceneViewFix = new CommandBuffer();
                //cmdSceneViewFix.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
                cmdSceneViewFix.SetGlobalTexture(m_DepthRTid, m_DepthRT);
                cmdSceneViewFix.Blit(m_DepthRT, BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial);
                context.ExecuteCommandBuffer(cmdSceneViewFix);
                cmdSceneViewFix.Clear();
                cmdSceneViewFix.ReleaseTemporaryRT(m_DepthRTid); //cleanup
                context.ExecuteCommandBuffer(cmdSceneViewFix);
                cmdSceneViewFix.Release();
            }
            #endif

            context.Submit();
        }
    }
}