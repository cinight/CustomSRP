using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE MAIN --------------------------------------------------------------------------------------------
//https://github.com/Unity-Technologies/Graphics/blob/9d6db83478c213bc066e5f728d94b7faa6662543/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/HDRenderPipeline.LightLoop.cs
public partial class SRP0802_RenderGraph : RenderPipeline
{
    private RenderGraph graph;
    private int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private RenderTargetIdentifier m_ColorRT;

    public SRP0802_RenderGraph()
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

            //Execute RenderGraph

            graph = new RenderGraph("SRP0802_RenderGraphPass");

            Render_SRP0802_BasePass(camera,graph,cull);
            CommandBuffer cmdRG = CommandBufferPool.Get("ExecuteRenderGraph");
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                commandBuffer = cmdRG,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            graph.Begin(rgParams);
            graph.Execute();
            context.ExecuteCommandBuffer(cmdRG);
            CommandBufferPool.Release(cmdRG);
            
            
                
            //Native Arrays for Attachaments
            // NativeArray<AttachmentDescriptor> renderPassAttachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
            // renderPassAttachments[0] = m_Albedo;
            // renderPassAttachments[1] = m_Emission;
            // renderPassAttachments[2] = m_Output;
            // renderPassAttachments[3] = m_Depth;
            // NativeArray<int> renderPassColorAttachments = new NativeArray<int>(2, Allocator.Temp);
            // renderPassColorAttachments[0] = 0;
            // renderPassColorAttachments[1] = 1;
            // NativeArray<int> renderPassOutputAttachments = new NativeArray<int>(1, Allocator.Temp);
            // renderPassOutputAttachments[0] = 2;



            //Camera Texture
            RenderTextureDescriptor camRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            camRTDesc.colorFormat = RenderTextureFormat.Default;
            camRTDesc.depthBufferBits = 24;
            camRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            camRTDesc.msaaSamples = 1;
            camRTDesc.enableRandomWrite = false;

            //Get Temp Texture for Color Texture
            m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_ColorRTid, camRTDesc,FilterMode.Bilinear);
            cmdTempId.SetRenderTarget(m_ColorRT); //so that result won't flip
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Color Texture
            // TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
            // bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            // colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
            // colorRTDesc.depthBufferBits = depthBufferBits;
            // colorRTDesc.msaaSamples = MSAASamples.None;
            // colorRTDesc.enableRandomWrite = false;

            //Depth Texture
            // TextureDesc depthRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
            // depthRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Depth,false);
            // depthRTDesc.depthBufferBits = 24;
            // depthRTDesc.msaaSamples = MSAASamples.None;
            // depthRTDesc.enableRandomWrite = false;
            //m_Depth = graph.CreateTexture(depthRTDesc);

            //Clear Attachements
            // m_Output.ConfigureTarget(m_ColorRT, false, true);
            // m_Output.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            // m_Albedo.ConfigureClear(camera.backgroundColor,1,0);
            // m_Emission.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            // m_Depth.ConfigureClear(new Color(),1,0);

            //Get Temp Texture for Color Texture
            //cmdTempId.SetRenderTarget(m_ColorRT); //so that result won't flip
            


            

           // graph.CompileRenderGraph();

            //Blit To Camera so that the CameraTarget has content and make sceneview works
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" BlitToCamera";
            cmd.Blit(m_ColorRT,BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release(); 

            //CleanUp Texture
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            //Submit the CommandBuffers
            context.Submit();

            //CleanUp NativeArrays
            // renderPassAttachments.Dispose();
            // renderPassColorAttachments.Dispose();
            // renderPassOutputAttachments.Dispose();

            graph.EndFrame();
            graph.Cleanup();
            graph = null;
            //graph.Cleanup();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}