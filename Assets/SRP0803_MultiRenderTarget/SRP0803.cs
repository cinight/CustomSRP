using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0803 : RenderPipelineAsset<SRP0803Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0803", priority = 1)]
    static void CreateSRP0803()
    {
        var instance = ScriptableObject.CreateInstance<SRP0803>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0803.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0803Instance();
    }
}

public class SRP0803Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0803_Pass"); //The shader pass tag just for SRP0803

    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    private static Material copyColorMaterial;

    private static int m_AlbedoRTid = Shader.PropertyToID("_CameraAlbedoTexture");
    private static int m_EmissionRTid = Shader.PropertyToID("_CameraEmissionTexture");
    private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture");
    private static RenderTargetIdentifier m_AlbedoRT = new RenderTargetIdentifier(m_AlbedoRTid);
    private static RenderTargetIdentifier m_EmissionRT = new RenderTargetIdentifier(m_EmissionRTid);
    private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);

    RenderTargetIdentifier[] mRTIDs = new RenderTargetIdentifier[2];

    public SRP0803Instance()
    {
        copyColorMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0803/copyColor"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
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

            //Texture Descriptor - Color
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            rtDesc.graphicsFormat = m_ColorFormat;
            rtDesc.depthBufferBits = 0;
            rtDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            rtDesc.msaaSamples = 1;
            rtDesc.enableRandomWrite = false;

            //Texture Descriptor - Depth
            RenderTextureDescriptor rtDescDepth = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            rtDescDepth.colorFormat = RenderTextureFormat.Depth;
            rtDescDepth.depthBufferBits = 24;
            rtDescDepth.msaaSamples = 1;
            rtDescDepth.enableRandomWrite = false;

            //Get Temp Texture for Color Texture
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
            cmdTempId.GetTemporaryRT(m_AlbedoRTid, rtDesc,FilterMode.Point);
            cmdTempId.GetTemporaryRT(m_EmissionRTid, rtDesc,FilterMode.Point);
            cmdTempId.GetTemporaryRT(m_DepthRTid, rtDescDepth,FilterMode.Point);
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Pass 1=========================================================

            //SetUp Multi-RenderTargets For the 1st pass & clear
            CommandBuffer cmdPass1 = new CommandBuffer();
            mRTIDs[0] = m_AlbedoRTid;
            mRTIDs[1] = m_EmissionRTid;
            cmdPass1.SetRenderTarget(mRTIDs,m_DepthRT);
            cmdPass1.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmdPass1);
            cmdPass1.Release();

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

            //Final blit====================================================

            //Blit to CameraTarget, to combine the previous 2 texture results with a blit material
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Cam:"+camera.name+" BlitToCamera";
            cmd.Blit(BuiltinRenderTextureType.CameraTarget,BuiltinRenderTextureType.CameraTarget,copyColorMaterial);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release(); 

            //CleanUp Texture
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_AlbedoRTid);
            cmdclean.ReleaseTemporaryRT(m_EmissionRTid);
            cmdclean.ReleaseTemporaryRT(m_DepthRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}