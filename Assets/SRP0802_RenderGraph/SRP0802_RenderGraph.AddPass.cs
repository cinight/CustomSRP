using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraph
{
    //input for add pass
    class SRP0802_AddPassData
    {
        public RendererListHandle m_Albedo;
        public RendererListHandle m_Emission;
    }

    //output for add pass
    public struct SRP0802_AddPassOut
    {
        public TextureHandle m_Output;
    }

    public SRP0802_AddPassOut Render_SRP0802_AddPass(Camera camera, RenderGraph graph, CullingResults cull)
    {
        ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802
        ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

        SRP0802_AddPassOut pout = new SRP0802_AddPassOut(); // remove this
        //{
        //     //Base Pass - Opaque
        //     using (var builder = graph.AddRenderPass<SRP0802_BasePassData>( "Base Pass", out SRP0802_BasePassData passData ) )
        //     {
        //         //Texture description
        //         TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        //         bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
        //         colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
        //         colorRTDesc.depthBufferBits = DepthBits.Depth24;
        //         colorRTDesc.msaaSamples = MSAASamples.None;
        //         colorRTDesc.enableRandomWrite = false;

        //         //Setup RendererListDesc
        //         RendererListDesc rendererDesc_base_Opaque = new RendererListDesc(m_PassName1,cull,camera);
        //         rendererDesc_base_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
        //         rendererDesc_base_Opaque.renderQueueRange = RenderQueueRange.opaque;
        //         RendererListHandle rHandle_base_Opaque = graph.CreateRendererList(rendererDesc_base_Opaque);

        //         RendererListDesc rendererDesc_base_Transparent = new RendererListDesc(m_PassName1,cull,camera);
        //         rendererDesc_base_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
        //         rendererDesc_base_Transparent.renderQueueRange = RenderQueueRange.transparent;
        //         RendererListHandle rHandle_base_Transparent= graph.CreateRendererList(rendererDesc_base_Transparent);

        //         RendererListDesc rendererDesc_add_Opaque = new RendererListDesc(m_PassName2,cull,camera);
        //         rendererDesc_add_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
        //         rendererDesc_add_Opaque.renderQueueRange = RenderQueueRange.opaque;
        //         RendererListHandle rHandle_add_Opaque = graph.CreateRendererList(rendererDesc_add_Opaque);

        //         RendererListDesc rendererDesc_add_Transparent = new RendererListDesc(m_PassName2,cull,camera);
        //         rendererDesc_add_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
        //         rendererDesc_add_Transparent.renderQueueRange = RenderQueueRange.transparent;
        //         RendererListHandle rHandle_add_Transparent = graph.CreateRendererList(rendererDesc_add_Transparent);

        //         passData.m_Albedo = graph.CreateTexture(colorRTDesc);
        //         passData.m_Albedo = builder.UseColorBuffer(passData.m_Albedo,0);
        //         //passData.m_Albedo = builder.WriteTexture(passData.m_Albedo);
        //         passData.m_Emission = graph.CreateTexture(colorRTDesc);
        //         passData.m_Emission = builder.UseColorBuffer(passData.m_Emission,1);
        //         //passData.m_Emission = builder.WriteTexture(passData.m_Emission);
        //         passData.RendererList = builder.UseRendererList(rHandle_base_Opaque);
                
        //         builder.SetRenderFunc((SRP0802_BasePassData data, RenderGraphContext context) => 
        //         {
        //             CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.RendererList );
        //         });

        //         //Output
        //         pout.m_Albedo = passData.m_Albedo;
        //         pout.m_Emission = passData.m_Emission;
        //     }
        // }

        return pout;
    }
}