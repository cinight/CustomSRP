using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph
{
    Material m_material;

    class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
    }

    public void Render_SRP0802_AddPass(Camera camera, RenderGraph graph, CullingResults cull, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RenderGraph/FinalColor"));

        using (var builder = graph.AddRenderPass<SRP0802_AddPassData>("Add Pass", out var passData, new ProfilingSampler("Add Pass Profiler" ) ) )
        {
            //Textures
            passData.m_Albedo = builder.ReadTexture(albedo);
            passData.m_Emission = builder.ReadTexture(emission);

            //Bind output to BackBuffer = render to screen
            builder.WriteTexture(graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));

            //Builder
            builder.SetRenderFunc((SRP0802_AddPassData data, RenderGraphContext context) => 
            {
                MaterialPropertyBlock mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                mpb.SetTexture("_CameraAlbedoTexture",data.m_Albedo);
                mpb.SetTexture("_CameraEmissionTexture",data.m_Emission);

                CoreUtils.SetRenderTarget( context.cmd, BuiltinRenderTextureType.CameraTarget );
                CoreUtils.DrawFullScreen( context.cmd, m_material, mpb );
            });
        }
    }
}