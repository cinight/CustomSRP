using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class SRP0802_RenderGraph : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802_RenderGraph", priority = 1)]
    static void CreateSRP0802_RenderGraph()
    {
        var instance = ScriptableObject.CreateInstance<SRP0802_RenderGraph>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802_RenderGraph.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802_RenderGraphInstance();
    }
}

//--------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraphPass
{
    //input for base pass
    class SRP0802_BasePassData
    {
        public RendererListHandle RendererList;
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
    }

    //output for base pass
    struct SRP0802_BasePassOut
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        //TextureHandle m_Depth;
    }

    //input for add pass
    class SRP0802_AddPassData
    {
        public RendererListHandle m_Albedo;
        public RendererListHandle m_Emission;
    }

    //output for add pass
    struct SRP0802_AddPassOut
    {
        public TextureHandle m_Output;
    }

    SRP0802_BasePassOut Render_SRP0802_BasePass(Camera camera, RenderGraph graph, CullingResults cull)
    {
        ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802
        ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

        SRP0802_BasePassOut basePassOut;
        {
            //Base Pass - Opaque
            using (var builder = graph.AddRenderPass<SRP0802_BasePassData>( "Base Pass", out SRP0802_BasePassData passData ) )
            {
                //Texture description
                TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
                bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
                colorRTDesc.depthBufferBits = DepthBits.Depth24;
                colorRTDesc.msaaSamples = MSAASamples.None;
                colorRTDesc.enableRandomWrite = false;

                //Setup RendererListDesc
                RendererListDesc rendererDesc_base_Opaque = new RendererListDesc(m_PassName1,cull,camera);
                rendererDesc_base_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
                rendererDesc_base_Opaque.renderQueueRange = RenderQueueRange.opaque;
                RendererListHandle rHandle_base_Opaque = graph.CreateRendererList(rendererDesc_base_Opaque);

                RendererListDesc rendererDesc_base_Transparent = new RendererListDesc(m_PassName1,cull,camera);
                rendererDesc_base_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
                rendererDesc_base_Transparent.renderQueueRange = RenderQueueRange.transparent;
                RendererListHandle rHandle_base_Transparent= graph.CreateRendererList(rendererDesc_base_Transparent);

                RendererListDesc rendererDesc_add_Opaque = new RendererListDesc(m_PassName2,cull,camera);
                rendererDesc_add_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
                rendererDesc_add_Opaque.renderQueueRange = RenderQueueRange.opaque;
                RendererListHandle rHandle_add_Opaque = graph.CreateRendererList(rendererDesc_add_Opaque);

                RendererListDesc rendererDesc_add_Transparent = new RendererListDesc(m_PassName2,cull,camera);
                rendererDesc_add_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
                rendererDesc_add_Transparent.renderQueueRange = RenderQueueRange.transparent;
                RendererListHandle rHandle_add_Transparent = graph.CreateRendererList(rendererDesc_add_Transparent);

                passData.m_Albedo = graph.CreateTexture(colorRTDesc);
                passData.m_Albedo = builder.UseColorBuffer(passData.m_Albedo,0);
                //passData.m_Albedo = builder.WriteTexture(passData.m_Albedo);
                passData.m_Emission = graph.CreateTexture(colorRTDesc);
                passData.m_Emission = builder.UseColorBuffer(passData.m_Emission,1);
                //passData.m_Emission = builder.WriteTexture(passData.m_Emission);
                passData.RendererList = builder.UseRendererList(rHandle_base_Opaque);
                
                builder.SetRenderFunc((SRP0802_BasePassData data, RenderGraphContext context) => 
                {
                    CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.RendererList );
                });

                //Output
                basePassOut.m_Albedo = passData.m_Albedo;
                basePassOut.m_Emission = passData.m_Emission;
            }
        }

        return basePassOut;
    }
}
//--------------------------------------------------------------------------------------------

public class SRP0802_RenderGraphInstance : RenderPipeline
{
    private RenderGraph graph;
    private int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private RenderTargetIdentifier m_ColorRT;

    public SRP0802_RenderGraphInstance()
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
            graph = new RenderGraph("SRP0802 RenderGraph");
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