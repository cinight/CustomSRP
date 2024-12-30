using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph_RasterCommandBuffer
{
    Material m_material;

    public class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
    }

    public void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RasterCommandBuffer/FinalColor"));
        
        TextureHandle cameraTarget = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);

        using (var builder = graph.AddRasterRenderPass<SRP0802_AddPassData>("Add Pass", out var passData, new ProfilingSampler("Add Pass Profiler" ) ) )
        {
            passData.m_Albedo = albedo;
            passData.m_Emission = emission;
            
            // Let RenderGraph know what textures we are reading and writing
            builder.UseTexture(albedo);
            builder.UseTexture(emission);
            builder.SetRenderAttachment(cameraTarget,0);
            
            // Set the render function
            builder.SetRenderFunc((SRP0802_AddPassData data, RasterGraphContext context) => 
            {
                m_material.SetTexture("_CameraAlbedoTexture",data.m_Albedo);
                m_material.SetTexture("_CameraEmissionTexture",data.m_Emission);

                Blitter.BlitTexture(context.cmd, graph.defaultResources.whiteTexture, Vector4.one,m_material,0);
            });
        }
    }
}