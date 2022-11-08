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

    AttachmentDescriptor m_Albedo = new AttachmentDescriptor(m_ColorFormat);
    AttachmentDescriptor m_Emission = new AttachmentDescriptor(m_ColorFormat);
    AttachmentDescriptor m_Output = new AttachmentDescriptor(m_ColorFormat);
    AttachmentDescriptor m_Depth = new AttachmentDescriptor(RenderTextureFormat.Depth);

    public SRP0802Instance()
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

            //Color Texture Descriptor
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.colorFormat = m_ColorFormat;
            colorRTDesc.depthBufferBits = depthBufferBits;
            colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            colorRTDesc.msaaSamples = 1;
            colorRTDesc.enableRandomWrite = false;

            //Get Temp Texture for Color Texture
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
            cmdTempId.SetRenderTarget(m_ColorRT); //so that result won't flip
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings1 = new DrawingSettings(m_PassName1, sortingSettings);
            DrawingSettings drawSettings2 = new DrawingSettings(m_PassName2, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Native Arrays for Attachaments
            NativeArray<AttachmentDescriptor> renderPassAttachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
            renderPassAttachments[0] = m_Albedo;
            renderPassAttachments[1] = m_Emission;
            renderPassAttachments[2] = m_Output;
            renderPassAttachments[3] = m_Depth;
            NativeArray<int> renderPassColorAttachments = new NativeArray<int>(2, Allocator.Temp);
            renderPassColorAttachments[0] = 0;
            renderPassColorAttachments[1] = 1;
            NativeArray<int> renderPassOutputAttachments = new NativeArray<int>(1, Allocator.Temp);
            renderPassOutputAttachments[0] = 2;

            //Clear Attachements
            m_Output.ConfigureTarget(m_ColorRT, false, true);
            m_Output.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            m_Albedo.ConfigureClear(camera.backgroundColor,1,0);
            m_Emission.ConfigureClear(new Color(0.0f, 0.0f, 0.0f, 0.0f),1,0);
            m_Depth.ConfigureClear(new Color(),1,0);
            
            //More clean to use ScopedRenderPass instead of BeginRenderPass+EndRenderPass
            using ( context.BeginScopedRenderPass(camera.pixelWidth, camera.pixelHeight,1,renderPassAttachments, 3) )
            {
                //Output to Albedo & Emission
                using ( context.BeginScopedSubPass(renderPassColorAttachments,false) )
                {
                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    CustomSRPUtil.RenderObjects("Render Opaque Objects to Albedo/Emission", context, cull, filterSettings, drawSettings1);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings1.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    CustomSRPUtil.RenderObjects("Render Transparent Objects to Albedo/Emission", context, cull, filterSettings, drawSettings1);
                }
                //Read from Albedo & Emission, then output to Output
                using ( context.BeginScopedSubPass(renderPassOutputAttachments,renderPassColorAttachments) )
                {
                    //Skybox
                    if(drawSkyBox)
                    {
                        CustomSRPUtil.RenderSkybox(context, camera);
                    }

                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    CustomSRPUtil.RenderObjects("Render Opaque Objects to Output", context, cull, filterSettings, drawSettings2);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings2.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    CustomSRPUtil.RenderObjects("Render Transparent Objects to Output", context, cull, filterSettings, drawSettings2);
                }
            }

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
            renderPassAttachments.Dispose();
            renderPassColorAttachments.Dispose();
            renderPassOutputAttachments.Dispose();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}