using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0502 : RenderPipelineAsset<SRP0502Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0502", priority = 1)]
    static void CreateSRP0502()
    {
        var instance = ScriptableObject.CreateInstance<SRP0502>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0502.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0502Instance();
    }
}

public class SRP0502Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0502_Pass"); //The shader pass tag just for SRP0502
    private static readonly ShaderTagId m_PassNameDistortion = new ShaderTagId("SRP0502_Distortion"); //The shader pass tag just for SRP0502

    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

    private int depthBufferBits = 24;

    public SRP0502Instance()
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
            colorRTDesc.graphicsFormat = m_ColorFormat;
            colorRTDesc.depthBufferBits = depthBufferBits;
            colorRTDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            colorRTDesc.msaaSamples = 1;
            colorRTDesc.enableRandomWrite = false;

            //Set Temp RT & set render target to the RT
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            DrawingSettings drawSettingsDistortion = new DrawingSettings(m_PassNameDistortion, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Skybox
            if(drawSkyBox)
            {
                CustomSRPUtil.RenderSkybox(context, camera);
            }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Opaque Objects", context, cull, filterSettings, drawSettings);


            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);

            //Blit to color texture
            CommandBuffer cmdBlitToTex = new CommandBuffer();
            cmdBlitToTex.name = "("+camera.name+")"+ "Blit to Color Texture";
            cmdBlitToTex.Blit(BuiltinRenderTextureType.CameraTarget,m_ColorRT);
            cmdBlitToTex.SetGlobalTexture(m_ColorRTid,m_ColorRT);
            cmdBlitToTex.SetRenderTarget(BuiltinRenderTextureType.CameraTarget); //Blit will change target, so make sure to reset it
            context.ExecuteCommandBuffer(cmdBlitToTex);
            cmdBlitToTex.Release();

            //Distortion object
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettingsDistortion.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Distortion Objects", context, cull, filterSettings, drawSettingsDistortion);

            //Clean Up
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}