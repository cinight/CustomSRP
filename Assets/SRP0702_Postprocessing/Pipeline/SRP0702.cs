using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[ExecuteInEditMode]
public class SRP0702 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0702", priority = 1)]
    static void CreateSRP0702()
    {
        var instance = ScriptableObject.CreateInstance<SRP0702>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0702.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0702Instance();
    }
}

public class SRP0702Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0702_Pass"); //The shader pass tag just for SRP0702

    private static Material depthOnlyMaterial;
    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatHDR = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatActive; //The one that is actually using
    private int depthBufferBits = 24;

    private static PostProcessRenderContext m_PostProcessRenderContext = new PostProcessRenderContext();
    private static PostProcessLayer m_CameraPostProcessLayer;

    public SRP0702Instance()
    {
        depthOnlyMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0702/DepthOnly"));
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

            //************************** Set TempRT ************************************

            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";

            //Color
            m_ColorFormatActive = camera.allowHDR ? m_ColorFormatHDR : m_ColorFormat;
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.graphicsFormat = m_ColorFormatActive;
            colorRTDesc.depthBufferBits = depthBufferBits;
            //colorRTDesc.sRGB = ;
            colorRTDesc.msaaSamples = camera.allowMSAA ? QualitySettings.antiAliasing : 1;
            colorRTDesc.enableRandomWrite = false;
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);

            //Depth
            RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            depthRTDesc.colorFormat = RenderTextureFormat.Depth;
            depthRTDesc.depthBufferBits = depthBufferBits;
            cmdTempId.GetTemporaryRT(m_DepthRTid, depthRTDesc,FilterMode.Bilinear);

            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //************************** Setup DrawSettings and FilterSettings ************************************

            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            DrawingSettings drawSettingsDepth = new DrawingSettings(m_PassName, sortingSettings)
            {
                perObjectData = PerObjectData.None,
                overrideMaterial = depthOnlyMaterial,
                overrideMaterialPassIndex = 0
            };

            //************************** Rendering depth ************************************

            //Set RenderTarget & Camera clear flag
            CommandBuffer cmdDepth = new CommandBuffer();
            cmdDepth.name = "("+camera.name+")"+ "Depth Clear Flag";
            cmdDepth.SetRenderTarget(m_DepthRT); //Set CameraTarget to the depth texture
            cmdDepth.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmdDepth);
            cmdDepth.Release();

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettingsDepth, ref filterSettings);

            //To let shader has _CameraDepthTexture, to make Depth of Field work
            CommandBuffer cmdDepthTexture = new CommandBuffer();
            cmdDepthTexture.name = "("+camera.name+")"+ "Depth Texture";
            cmdDepthTexture.SetGlobalTexture(m_DepthRTid,m_DepthRT);
            context.ExecuteCommandBuffer(cmdDepthTexture);
            cmdDepthTexture.Release();

            //************************** Rendering colors ************************************
            
            //Set RenderTarget & Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "("+camera.name+")"+ "Clear Flag";
            cmd.SetRenderTarget(m_ColorRT); //Set CameraTarget to the color texture
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //************************** Rendering Opaque Objects ************************************

            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //************************** Opaque Post-processing ************************************
            //Ambient Occlusion, 
            PostProcessLayer m_CameraPostProcessLayer = camera.GetComponent<PostProcessLayer>();
            bool usePostProcessing = m_CameraPostProcessLayer != null && m_CameraPostProcessLayer.enabled;

            if(usePostProcessing)
            {
                CommandBuffer cmdpp = new CommandBuffer();
                cmdpp.name = "("+camera.name+")"+ "Post-processing Opaque";

                m_PostProcessRenderContext.Reset();
                m_PostProcessRenderContext.camera = camera;
                m_PostProcessRenderContext.source = m_ColorRT;
                m_PostProcessRenderContext.sourceFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetRenderTextureFormat(m_ColorFormatActive);
                m_PostProcessRenderContext.destination = m_ColorRT;
                m_PostProcessRenderContext.command = cmdpp;
                m_PostProcessRenderContext.flip = camera.targetTexture == null;
                m_CameraPostProcessLayer.RenderOpaqueOnly(m_PostProcessRenderContext);
               
                context.ExecuteCommandBuffer(cmdpp);
                cmdpp.Release();
            }

            //************************** Rendering Transparent Objects ************************************

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //************************** Transparent Post-processing ************************************
            //Bloom, Vignette, Grain, ColorGrading, LensDistortion, Chromatic Aberration, Auto Exposure
            if(usePostProcessing)
            {
                CommandBuffer cmdpp = new CommandBuffer();
                cmdpp.name = "("+camera.name+")"+ "Post-processing Transparent";

                m_PostProcessRenderContext.Reset();
                m_PostProcessRenderContext.camera = camera;
                m_PostProcessRenderContext.source = m_ColorRT;
                m_PostProcessRenderContext.sourceFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetRenderTextureFormat(m_ColorFormatActive);
                m_PostProcessRenderContext.destination = BuiltinRenderTextureType.CameraTarget;
                m_PostProcessRenderContext.command = cmdpp;
                m_PostProcessRenderContext.flip = camera.targetTexture == null;
                m_CameraPostProcessLayer.Render(m_PostProcessRenderContext);
                
                context.ExecuteCommandBuffer(cmdpp);
                cmdpp.Release();
            }

            //************************** Make sure screen has the thing when Postprocessing is off ************************************
            if(!usePostProcessing)
            {
                CommandBuffer cmdBlitToCam = new CommandBuffer();
                cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
                cmdBlitToCam.Blit(m_ColorRTid, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmdBlitToCam);
                cmdBlitToCam.Release();
            }

            //************************** Clean Up ************************************
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            cmdclean.ReleaseTemporaryRT(m_DepthRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();
        }
    }
}