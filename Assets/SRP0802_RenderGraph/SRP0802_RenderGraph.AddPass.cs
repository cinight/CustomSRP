using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph
{
    Material m_material;

    class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public TextureHandle m_CameraTarget;
    }

    public void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RenderGraph/FinalColor"));

        using (var builder = graph.AddRenderPass<SRP0802_AddPassData>("Add Pass", out var passData, new ProfilingSampler("Add Pass Profiler" ) ) )
        {
            //Textures
            passData.m_Albedo = builder.ReadTexture(albedo);
            passData.m_Emission = builder.ReadTexture(emission);
            passData.m_CameraTarget = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
            
            // Let RenderGraph know what textures we are reading and writing (visible on Render Graph viewer)
            builder.UseColorBuffer(passData.m_CameraTarget,0);
            
            //Builder
            builder.SetRenderFunc((SRP0802_AddPassData data, RenderGraphContext context) => 
            {
                m_material.SetTexture("_CameraAlbedoTexture",data.m_Albedo);
                m_material.SetTexture("_CameraEmissionTexture",data.m_Emission);
                context.cmd.Blit( null, passData.m_CameraTarget, m_material ); //
            });
        }
    }
}