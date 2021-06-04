using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraph
{
    ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

    class SRP0802_AddPassData
    {
        public RendererListHandle m_renderList_opaque;
        public RendererListHandle m_renderList_transparent;
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public RendererListHandle m_renderList;
    }

    // private TextureHandle CreateOutputTexture(RenderGraph graph)
    // {
    //     bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

    //     return graph.CreateTexture(new TextureDesc(Vector2.one)
    //     {
    //         colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB),
    //         enableRandomWrite = false,
    //         msaaSamples = MSAASamples.None,
    //         depthBufferBits = DepthBits.Depth24,
    //         clearBuffer = true,
    //         clearColor = Color.black,
    //         name = "AddPassOutput"
    //     });
    // }

    public void Render_SRP0802_AddPass(Camera camera, RenderGraph graph, CullingResults cull, TextureHandle albedo, TextureHandle emission)
    {
        using (var builder = graph.AddRenderPass<SRP0802_AddPassData>("Add Pass", out var passData))
        {
            //Textures
            passData.m_Albedo = builder.ReadTexture(albedo);
            passData.m_Emission = builder.ReadTexture(emission);

            //Bind output to BackBuffer = render to screen
            builder.WriteTexture(graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
            
            //Renderes
            RendererListDesc rendererDesc_add_Opaque = new RendererListDesc(m_PassName2,cull,camera);
            rendererDesc_add_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererDesc_add_Opaque.renderQueueRange = RenderQueueRange.opaque;
            RendererListHandle rHandle_add_Opaque = graph.CreateRendererList(rendererDesc_add_Opaque);
            passData.m_renderList_opaque = builder.UseRendererList(rHandle_add_Opaque);

            RendererListDesc rendererDesc_add_Transparent = new RendererListDesc(m_PassName2,cull,camera);
            rendererDesc_add_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
            rendererDesc_add_Transparent.renderQueueRange = RenderQueueRange.transparent;
            RendererListHandle rHandle_add_Transparent = graph.CreateRendererList(rendererDesc_add_Transparent);
            passData.m_renderList_transparent = builder.UseRendererList(rHandle_add_Transparent);

            //Builder
            builder.SetRenderFunc((SRP0802_AddPassData data, RenderGraphContext context) => 
            {
                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_renderList_opaque );
                CoreUtils.DrawRendererList( context.renderContext, context.cmd, data.m_renderList_transparent );
            });

            //return texOut;
        }
    }
}