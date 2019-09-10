using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP0403
{
    public class SRP0403 : RenderPipeline
    {
        private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0403_Pass"); //The shader pass tag just for SRP0403

        private static SRP0403_Asset m_PipelineAsset;
        private static int _kernel;

        private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
        private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
        private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

        private static Material depthOnlyMaterial;
        private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture");
        private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);
        private int depthBufferBits = 24;

        public SRP0403(SRP0403_Asset pipelineAsset)
        {
            m_PipelineAsset = pipelineAsset;
            //if(m_PipelineAsset.computeShader != null) _kernel = m_PipelineAsset.computeShader.FindKernel ("CSMain");
            _kernel = 0;
            depthOnlyMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0403/DepthOnly"));
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

                //Color Texture Descriptor
                RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                colorRTDesc.graphicsFormat = m_ColorFormat;
                colorRTDesc.depthBufferBits = depthBufferBits;
                colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                colorRTDesc.msaaSamples = 1;
                colorRTDesc.enableRandomWrite = true; //For compute

                //Depth Texture Descriptor
                RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                depthRTDesc.colorFormat = RenderTextureFormat.Depth;
                depthRTDesc.depthBufferBits = depthBufferBits;

                //Set texture temp RT
                CommandBuffer cmdTempId = new CommandBuffer();
                cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
                cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
                cmdTempId.GetTemporaryRT(m_DepthRTid, depthRTDesc,FilterMode.Bilinear);
                context.ExecuteCommandBuffer(cmdTempId);
                cmdTempId.Release();

                //Setup DrawSettings and FilterSettings
                var sortingSettings = new SortingSettings(camera);
                DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
                FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);
                DrawingSettings drawSettingsDepth = new DrawingSettings(m_PassName, sortingSettings)
                {
                    perObjectData = PerObjectData.None,
                    overrideMaterial = depthOnlyMaterial,
                    overrideMaterialPassIndex = 0
                };

                //Clear Depth Texture
                CommandBuffer cmdDepth = new CommandBuffer();
                cmdDepth.name = "("+camera.name+")"+ "Depth Clear Flag";
                cmdDepth.SetRenderTarget(m_DepthRT); //Set CameraTarget to the depth texture
                cmdDepth.ClearRenderTarget(true, true, Color.black);
                context.ExecuteCommandBuffer(cmdDepth);
                cmdDepth.Release();

                //Draw Depth with Opaque objects
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                drawSettingsDepth.sortingSettings = sortingSettings;
                filterSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(cull, ref drawSettingsDepth, ref filterSettings);

                //Draw Depth with Transparent objects
                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                drawSettingsDepth.sortingSettings = sortingSettings;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(cull, ref drawSettingsDepth, ref filterSettings);

                //To let shader has _CameraDepthTexture
                CommandBuffer cmdDepthTexture = new CommandBuffer();
                cmdDepthTexture.name = "("+camera.name+")"+ "Depth Texture";
                cmdDepthTexture.SetGlobalTexture(m_DepthRTid,m_DepthRT);
                context.ExecuteCommandBuffer(cmdDepthTexture);
                cmdDepthTexture.Release();

                //Camera clear flag
                CommandBuffer cmd = new CommandBuffer();
                cmd.SetRenderTarget(m_ColorRT); //Remember to set target
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

                //Run Compute shader
                if(m_PipelineAsset.computeShader != null)
                {
                    CommandBuffer cmdCompute = new CommandBuffer();
                    cmdCompute.name = "("+camera.name+")"+ "Compute";
                    cmdCompute.SetComputeIntParam(m_PipelineAsset.computeShader, "range",m_PipelineAsset.detectRange);
                    cmdCompute.SetComputeFloatParam(m_PipelineAsset.computeShader, "detect",m_PipelineAsset.edgeDetect);
                    cmdCompute.SetComputeTextureParam(m_PipelineAsset.computeShader, _kernel, m_ColorRTid,m_ColorRT);
                    cmdCompute.SetComputeTextureParam(m_PipelineAsset.computeShader, _kernel, m_DepthRTid,m_DepthRT);
                    cmdCompute.DispatchCompute(m_PipelineAsset.computeShader, _kernel, camera.pixelWidth / 8 + 1, camera.pixelHeight / 8 + 1, 1);
                    context.ExecuteCommandBuffer(cmdCompute);
                    cmdCompute.Release();
                }

                //Blit the content back to screen
                CommandBuffer cmdBlitToCam = new CommandBuffer();
                cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
                cmdBlitToCam.Blit(m_ColorRT, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmdBlitToCam);
                cmdBlitToCam.Release();

                //Clean Up
                CommandBuffer cmdclean = new CommandBuffer();
                cmdclean.name = "("+camera.name+")"+ "Clean Up";
                cmdclean.ReleaseTemporaryRT(m_DepthRTid);
                cmdclean.ReleaseTemporaryRT(m_ColorRTid);
                context.ExecuteCommandBuffer(cmdclean);
                cmdclean.Release();

                context.Submit();
            }
        }
    }
}

