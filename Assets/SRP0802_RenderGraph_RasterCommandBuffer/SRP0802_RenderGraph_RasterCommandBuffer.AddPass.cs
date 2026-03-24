using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
    }

    private static Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
    private RTHandle cameraTargetHandle;
    private RenderTargetInfo cameraTargetHandleInfo;
    public void SetUpAddPass(Camera camera)
    {
        //TextureHandle cameraTarget = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
        if(cameraTargetHandle == null)
        {
            cameraTargetHandle = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);
        }

        //Texture description
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
        cameraTargetHandleInfo = new RenderTargetInfo()
        {
            format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB),
            height = camera.pixelHeight,
            width = camera.pixelWidth,
            bindMS = false,
            msaaSamples = 1,
            volumeDepth = 1,
        };
    }

    public void CleanupAddPass()
    {
        cameraTargetHandle?.Release();
    }

    public void Render_SRP0802_AddPass(RenderGraph graph, TextureHandle albedo, TextureHandle emission)
    {
        if(m_material == null) m_material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/CustomSRP/SRP0802_RasterCommandBuffer/FinalColor"));
        
        TextureHandle cameraTarget = graph.ImportTexture(cameraTargetHandle, cameraTargetHandleInfo);

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

                Blitter.BlitTexture(context.cmd, data.m_Albedo, m_ScaleBias ,m_material,0);
            });
        }
    }
}