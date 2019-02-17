using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP1002 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP1002", priority = 1)]
    static void CreateSRP1002()
    {
        var instance = ScriptableObject.CreateInstance<SRP1002>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP1002.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP1002Instance();
    }
}

public class SRP1002Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP1002_Pass"); //The shader pass tag just for SRP1002

    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

    private int depthBufferBits = 24;

    public SRP1002Instance()
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

            //Color Texture Descriptor
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.graphicsFormat = m_ColorFormat;
            colorRTDesc.depthBufferBits = depthBufferBits;
            colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            colorRTDesc.msaaSamples = 1;
            colorRTDesc.enableRandomWrite = false;

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //SetUp CommandBuffer
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "("+camera.name+")"+ "Rendering";

            //Set Temp RT & set render target to the RT
            //CommandBuffer cmdTempId = new CommandBuffer();
            //cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmd.Clear();
            cmd.BeginSample("GetTemporaryRT");
                cmd.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
            cmd.EndSample("GetTemporaryRT");
            cmd.BeginSample("SetRenderTarget");
                cmd.SetRenderTarget(m_ColorRT);
            cmd.EndSample("SetRenderTarget");
            context.ExecuteCommandBuffer(cmd);
            //cmdTempId.Release();

            //Camera clear flag
            //CommandBuffer cmd = new CommandBuffer();
            //cmd.name = "("+camera.name+")"+ "Clear Flag";
            cmd.Clear();
            cmd.BeginSample("ClearRenderTarget");
                cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            cmd.EndSample("ClearRenderTarget");
            context.ExecuteCommandBuffer(cmd);
            //cmd.Release();

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

            //Blit to screen
            //CommandBuffer cmdBlitToScreen = new CommandBuffer();
            //cmdBlitToScreen.name = "("+camera.name+")"+ "Blit to Screen";
            cmd.Clear();
            cmd.BeginSample("Blit");
                cmd.Blit(m_ColorRT,BuiltinRenderTextureType.CameraTarget);
            cmd.EndSample("Blit");
            context.ExecuteCommandBuffer(cmd);
            //cmd.Release();

            //Clean Up
            //CommandBuffer cmdclean = new CommandBuffer();
            //cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmd.Clear();
            cmd.BeginSample("ReleaseTemporaryRT");
                cmd.ReleaseTemporaryRT(m_ColorRTid);
            cmd.EndSample("ReleaseTemporaryRT");
            context.ExecuteCommandBuffer(cmd);

            //Finish commandBuffer
            cmd.Release();

            context.Submit();
        }
    }
}