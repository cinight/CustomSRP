using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0701 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0701", priority = 1)]
    static void CreateSRP0701()
    {
        var instance = ScriptableObject.CreateInstance<SRP0701>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0701.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0701Instance();
    }
}

public class SRP0701Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0701_Pass"); //The shader pass tag just for SRP0701

    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorRT");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatHDR = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    private int depthBufferBits = 24;

    public SRP0701Instance()
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

            //************************** Start Set TempRT ************************************
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";

            //Color
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.graphicsFormat = camera.allowHDR ? m_ColorFormatHDR : m_ColorFormat;
            colorRTDesc.depthBufferBits = depthBufferBits;
            //colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            colorRTDesc.msaaSamples = camera.allowMSAA ? QualitySettings.antiAliasing : 1;
            colorRTDesc.enableRandomWrite = false;
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);

            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //************************** End Set TempRT ************************************

            
            //Set RenderTarget & Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.SetRenderTarget(m_ColorRT); //Set CameraTarget to the color texture
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

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

            //Blit the content back to screen
            CommandBuffer cmdBlitToCam = new CommandBuffer();
            cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
            cmdBlitToCam.Blit(m_ColorRTid, BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmdBlitToCam);
            cmdBlitToCam.Release();

            //************************** Clean Up ************************************
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();
        }
    }
}