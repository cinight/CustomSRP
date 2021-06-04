using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE BASE PASS --------------------------------------------------------------------------------------------
// This pass renders objects into 2 RenderTargets:
// Albedo - grey texture
// Emission - animated color
public partial class SRP0802_RenderGraph
{
    ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802

    public class SRP0802_BasePassData
    {
        public RendererListHandle m_renderList_opaque;
        public RendererListHandle m_renderList_transparent;
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public TextureHandle m_Depth;
    }

    private TextureHandle CreateAlbedoTexture(RenderGraph graph, Camera camera)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
        colorRTDesc.depthBufferBits = 0;
        colorRTDesc.msaaSamples = MSAASamples.None;
        colorRTDesc.enableRandomWrite = false;
        colorRTDesc.clearBuffer = true;
        colorRTDesc.clearColor = Color.black;
        colorRTDesc.name = "Albedo";

        //Create texture
        TextureHandle tex = graph.CreateTexture(colorRTDesc);

        return tex;
    }

    private TextureHandle CreateEmissionTexture(RenderGraph graph, Camera camera)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
        colorRTDesc.depthBufferBits = 0;
        colorRTDesc.msaaSamples = MSAASamples.None;
        colorRTDesc.enableRandomWrite = false;
        colorRTDesc.clearBuffer = true;
        colorRTDesc.clearColor = Color.black;
        colorRTDesc.name = "Emission";

        //Create texture
        TextureHandle tex = graph.CreateTexture(colorRTDesc);

        return tex;
    }

    private TextureHandle CreateDepthTexture(RenderGraph graph, Camera camera)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Depth,colorRT_sRGB);
        colorRTDesc.depthBufferBits = DepthBits.Depth24;
        colorRTDesc.msaaSamples = MSAASamples.None;
        colorRTDesc.enableRandomWrite = false;
        colorRTDesc.clearBuffer = true;
        colorRTDesc.clearColor = Color.black;
        colorRTDesc.name = "Depth";

        //Create texture
        TextureHandle tex = graph.CreateTexture(colorRTDesc);

        return tex;
    }

    public SRP0802_BasePassData Render_SRP0802_BasePass(Camera camera, RenderGraph graph, CullingResults cull)
    {
        using (var builder = graph.AddRenderPass<SRP0802_BasePassData>( "Base Pass", out var passData, new ProfilingSampler("Base Pass Profiler" ) ) )
        {
            //Textures
            TextureHandle Albedo = CreateAlbedoTexture(graph,camera);
            passData.m_Albedo = builder.UseColorBuffer(Albedo,0);
            TextureHandle Emission = CreateEmissionTexture(graph,camera);
            passData.m_Emission = builder.UseColorBuffer(Emission,1);
            TextureHandle Depth = CreateDepthTexture(graph,camera);
            passData.m_Depth = builder.UseDepthBuffer(Depth, DepthAccess.Write);

            //Renderers
            RendererListDesc rendererDesc_base_Opaque = new RendererListDesc(m_PassName1,cull,camera);
            rendererDesc_base_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererDesc_base_Opaque.renderQueueRange = RenderQueueRange.opaque;
            RendererListHandle rHandle_base_Opaque = graph.CreateRendererList(rendererDesc_base_Opaque);
            passData.m_renderList_opaque = builder.UseRendererList(rHandle_base_Opaque);

            RendererListDesc rendererDesc_base_Transparent = new RendererListDesc(m_PassName1,cull,camera);
            rendererDesc_base_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
            rendererDesc_base_Transparent.renderQueueRange = RenderQueueRange.transparent;
            RendererListHandle rHandle_base_Transparent= graph.CreateRendererList(rendererDesc_base_Transparent);
            passData.m_renderList_transparent = builder.UseRendererList(rHandle_base_Transparent);
            
            //Builder
            builder.SetRenderFunc((SRP0802_BasePassData data, RenderGraphContext context) => 
            {
                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_renderList_opaque );
                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_renderList_transparent );
            });

            return passData;
        }
    }
}