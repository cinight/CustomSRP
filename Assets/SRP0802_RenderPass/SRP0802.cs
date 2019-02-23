using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0802 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802", priority = 1)]
    static void CreateSRP0802()
    {
        var instance = ScriptableObject.CreateInstance<SRP0802>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802Instance();
    }
}

public class SRP0802Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802
    private static readonly ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

    private static RenderTextureFormat m_ColorFormat = RenderTextureFormat.Default;
    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private int depthBufferBits = 24;

    //private static Material copyColorMaterial;

    public SRP0802Instance()
    {
        // copyColorMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0802/CopyColor"))
        // {
        //     hideFlags = HideFlags.HideAndDontSave
        // };
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(camera);

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

            //Color Texture Descriptor
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.colorFormat = m_ColorFormat;
            colorRTDesc.depthBufferBits = depthBufferBits;
            //colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            colorRTDesc.msaaSamples = 1;
            colorRTDesc.enableRandomWrite = false;

            //Get Temp Texture for Color Texture
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
            cmdTempId.SetRenderTarget(m_ColorRT);
            //cmdTempId.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings1 = new DrawingSettings(m_PassName1, sortingSettings);
            DrawingSettings drawSettings2 = new DrawingSettings(m_PassName2, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            AttachmentDescriptor m_Albedo = new AttachmentDescriptor(m_ColorFormat);
            AttachmentDescriptor m_Emission = new AttachmentDescriptor(m_ColorFormat);
            AttachmentDescriptor m_Combine = new AttachmentDescriptor(m_ColorFormat);
            m_Combine.ConfigureTarget(m_ColorRT, false, true);
            //AttachmentDescriptor m_Output = new AttachmentDescriptor(m_ColorFormat);
            AttachmentDescriptor m_Depth = new AttachmentDescriptor(RenderTextureFormat.Depth);
            

            //Native Arrays for Attachaments
            NativeArray<AttachmentDescriptor> renderPassAttachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
            renderPassAttachments[0] = m_Albedo;
            renderPassAttachments[1] = m_Emission;
            renderPassAttachments[2] = m_Combine;
            //renderPassAttachments[3] = m_Output;
            renderPassAttachments[3] = m_Depth;
            NativeArray<int> renderPassColorAttachments = new NativeArray<int>(2, Allocator.Temp);
            renderPassColorAttachments[0] = 0;
            renderPassColorAttachments[1] = 1;
            NativeArray<int> renderPassCombineAttachments = new NativeArray<int>(1, Allocator.Temp);
            renderPassCombineAttachments[0] = 2;
            //NativeArray<int> renderPassOutputAttachments = new NativeArray<int>(1, Allocator.Temp);
            //renderPassOutputAttachments[0] = 3;

            // Camera clear flag
            m_Albedo.ConfigureClear(camera.backgroundColor,1,0);
            m_Emission.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            m_Depth.ConfigureClear(new Color(),1,0);
            m_Combine.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            //m_Output.ConfigureClear(Color.black,1,0);
            
            //If we want to use the old way, we can use ScopedRenderPass
            //using (context.BeginScopedRenderPass(...)) { ... }
            using ( context.BeginScopedRenderPass(camera.pixelWidth, camera.pixelHeight,1,renderPassAttachments, 3) )
            {
                using ( context.BeginScopedSubPass(renderPassColorAttachments,false) )
                {
                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);
                }
                using ( context.BeginScopedSubPass(renderPassCombineAttachments,renderPassColorAttachments) )
                {
                    //Skybox
                    if(drawSkyBox)  {  context.DrawSkybox(camera);  }

                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);
                }
                // using ( context.BeginScopedSubPass(renderPassOutputAttachments,renderPassCombineAttachments) )
                // {
                //     //Blit back to CameraTarget

                // }
            }

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" BlitToColorTexture";
            cmd.Blit(m_ColorRT,BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release(); 

            //CleanUp
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            //Submit the CommandBuffers
            context.Submit();

            //CleanUp
            renderPassAttachments.Dispose();
            renderPassColorAttachments.Dispose();
            renderPassCombineAttachments.Dispose();
            //renderPassOutputAttachments.Dispose();

            
        }
    }
}