using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE ADD PASS --------------------------------------------------------------------------------------------
// This pass does a image effect that Albedo + Emission = final color
public partial class SRP0802_RenderGraph_RasterCommandBuffer
{
    Material m_material;

    public class SRP0802_AddPassData
    {
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public TextureHandle m_CameraTarget;
    }

    public void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RasterCommandBuffer/FinalColor"));

        using (var builder = graph.AddRasterRenderPass<SRP0802_AddPassData>("Add Pass", out var passData, new ProfilingSampler("Add Pass Profiler" ) ) )
        {
            //Input Textures
            passData.m_Albedo = builder.UseTexture(albedo,IBaseRenderGraphBuilder.AccessFlags.Read);
            passData.m_Emission = builder.UseTexture(emission,IBaseRenderGraphBuilder.AccessFlags.Read);

            //Target backbuffer
            TextureHandle cameraTarget = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
            passData.m_CameraTarget = builder.UseTextureFragment(cameraTarget,0,IBaseRenderGraphBuilder.AccessFlags.Write);
            
            //Builder
            builder.SetRenderFunc((SRP0802_AddPassData data, RasterGraphContext context) => 
            {
                m_material.SetTexture("_CameraAlbedoTexture",data.m_Albedo);
                m_material.SetTexture("_CameraEmissionTexture",data.m_Emission);

                Blitter.BlitTexture(context.cmd, graph.defaultResources.whiteTexture, Vector4.one,m_material,0);
            });
        }
    }
}