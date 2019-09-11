using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[ExecuteInEditMode]
public class SRP0703 : RenderPipelineAsset
{
    public bool motionVectorDebug = false;

    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0703", priority = 1)]
    static void CreateSRP0703()
    {
        var instance = ScriptableObject.CreateInstance<SRP0703>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0703.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0703Instance(motionVectorDebug);
    }
}

public class SRP0703Instance : RenderPipeline
{
    private bool _motionVectorDebug;

    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0703_Pass"); //The shader pass tag just for SRP0703

    private static Material depthOnlyMaterial;
    private static Material motionVectorMaterial;

    private static Material motionVectorDebugMaterial; //For Debug

    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture");
    private static int m_MotionVectorRTid = Shader.PropertyToID("_CameraMotionVectorsTexture");

    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);
    private static RenderTargetIdentifier m_MotionVectorRT = new RenderTargetIdentifier(m_MotionVectorRTid);
    private int depthBufferBits = 24;

    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatHDR = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatActive; //The one that is actually using

    //For camera motion vector
    private Matrix4x4 _NonJitteredVP;
    private Matrix4x4 _PreviousVP;
    static Mesh s_FullscreenMesh = null;
    public static Mesh fullscreenMesh
    {
        get
        {
            if (s_FullscreenMesh != null)
                return s_FullscreenMesh;

            float topV = 1.0f;
            float bottomV = 0.0f;

            s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
            s_FullscreenMesh.SetVertices(new List<Vector3>
            {
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(1.0f,  1.0f, 0.0f)
            });

            s_FullscreenMesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0.0f, bottomV),
                new Vector2(0.0f, topV),
                new Vector2(1.0f, bottomV),
                new Vector2(1.0f, topV)
            });

            s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
            s_FullscreenMesh.UploadMeshData(true);
            return s_FullscreenMesh;
        }
    }

    public SRP0703Instance(bool motionVectorDebug)
    {
        depthOnlyMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0703/DepthOnly"));
        motionVectorMaterial = new Material(Shader.Find("Hidden/CustomSRP/SRP0703/MotionVectors"));

        _motionVectorDebug = motionVectorDebug;
        motionVectorDebugMaterial = new Material(Shader.Find("MotionVectorsDebug"));
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

            //************************** Set TempRT ************************************

            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";

            //Color
            m_ColorFormatActive = camera.allowHDR ? m_ColorFormatHDR : m_ColorFormat;
            RenderTextureDescriptor colorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.graphicsFormat = m_ColorFormatActive;
            colorRTDesc.depthBufferBits = depthBufferBits;
            //colorRTDesc.sRGB = ;
            colorRTDesc.msaaSamples = camera.allowMSAA ? QualitySettings.antiAliasing : 1;
            colorRTDesc.enableRandomWrite = false;
            cmdTempId.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);

            //Depth
            RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            depthRTDesc.colorFormat = RenderTextureFormat.Depth;
            depthRTDesc.depthBufferBits = depthBufferBits;
            cmdTempId.GetTemporaryRT(m_DepthRTid, depthRTDesc,FilterMode.Bilinear);

            //MotionVector
            RenderTextureDescriptor motionvectorRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            motionvectorRTDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SFloat;
            motionvectorRTDesc.depthBufferBits = depthBufferBits;
            //colorRTDesc.sRGB = ;
            motionvectorRTDesc.msaaSamples = 1;
            motionvectorRTDesc.enableRandomWrite = false;
            cmdTempId.GetTemporaryRT(m_MotionVectorRTid, motionvectorRTDesc,FilterMode.Bilinear);

            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

           //************************** Setup DrawSettings and FilterSettings ************************************

            camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            var sortingSettings = new SortingSettings(camera);

            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            DrawingSettings drawSettingsMotionVector = new DrawingSettings(m_PassName, sortingSettings)
            {
                perObjectData = PerObjectData.MotionVectors,
                overrideMaterial = motionVectorMaterial,
                overrideMaterialPassIndex = 0
            };
            FilteringSettings filterSettingsMotionVector = new FilteringSettings(RenderQueueRange.all)
            {
                excludeMotionVectorObjects = false
            };

            DrawingSettings drawSettingsDepth = new DrawingSettings(m_PassName, sortingSettings)
            {
                //perObjectData = PerObjectData.None,
                overrideMaterial = depthOnlyMaterial,
                overrideMaterialPassIndex = 0,
            };
            FilteringSettings filterSettingsDepth = new FilteringSettings(RenderQueueRange.all);

            //************************** Rendering depth ************************************

            //Set RenderTarget & Camera clear flag
            CommandBuffer cmdDepth = new CommandBuffer();
            cmdDepth.name = "("+camera.name+")"+ "Depth Clear Flag";
            cmdDepth.SetRenderTarget(m_DepthRT); //Set CameraTarget to the depth texture
            cmdDepth.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmdDepth);
            cmdDepth.Release();

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettingsDepth.sortingSettings = sortingSettings;
            filterSettingsDepth.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettingsDepth, ref filterSettingsDepth);

            //To let shader has _CameraDepthTexture
            CommandBuffer cmdDepthTexture = new CommandBuffer();
            cmdDepthTexture.name = "("+camera.name+")"+ "Depth Texture";
            cmdDepthTexture.SetGlobalTexture(m_DepthRTid,m_DepthRT);
            context.ExecuteCommandBuffer(cmdDepthTexture);
            cmdDepthTexture.Release();

            //************************** Rendering motion vectors ************************************

            //Camera clear flag
            CommandBuffer cmdMotionvector = new CommandBuffer();
            cmdMotionvector.SetRenderTarget(m_MotionVectorRT); //Set CameraTarget to the motion vector texture
            cmdMotionvector.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmdMotionvector);
            cmdMotionvector.Release();

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettingsMotionVector.sortingSettings = sortingSettings;
            filterSettingsMotionVector.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettingsMotionVector, ref filterSettingsMotionVector);

            //Camera motion vector
            CommandBuffer cmdCameraMotionVector = new CommandBuffer();
            cmdCameraMotionVector.name = "("+camera.name+")"+ "Camera MotionVector";
            _NonJitteredVP = camera.nonJitteredProjectionMatrix * camera.worldToCameraMatrix;
            cmdCameraMotionVector.SetGlobalMatrix("_CamPrevViewProjMatrix", _PreviousVP );
            cmdCameraMotionVector.SetGlobalMatrix("_CamNonJitteredViewProjMatrix",  _NonJitteredVP);
            cmdCameraMotionVector.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmdCameraMotionVector.DrawMesh( fullscreenMesh, Matrix4x4.identity, motionVectorMaterial, 0, 1, null); // draw full screen quad to make Camera motion
            cmdCameraMotionVector.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            context.ExecuteCommandBuffer(cmdCameraMotionVector);
            cmdCameraMotionVector.Release();
            
            //To let shader has MotionVectorTexture
            CommandBuffer cmdMotionVectorTexture = new CommandBuffer();
            cmdMotionVectorTexture.name = "("+camera.name+")"+ "MotionVector Texture";            
            cmdMotionVectorTexture.SetGlobalTexture(m_MotionVectorRTid,m_MotionVectorRT);
            context.ExecuteCommandBuffer(cmdMotionVectorTexture);
            cmdMotionVectorTexture.Release();

            //************************** Rendering color ************************************

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.SetRenderTarget(m_ColorRT); //Set CameraTarget to the color texture
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //************************** SetUp Post-processing ************************************
            
            PostProcessLayer m_CameraPostProcessLayer = camera.GetComponent<PostProcessLayer>();
            bool hasPostProcessing = m_CameraPostProcessLayer != null;
            bool usePostProcessing =  false;
            //bool hasOpaqueOnlyEffects = false;
            PostProcessRenderContext m_PostProcessRenderContext = null;
            if(hasPostProcessing)
            {
                m_PostProcessRenderContext = new PostProcessRenderContext();
                usePostProcessing =  m_CameraPostProcessLayer.enabled;
                //hasOpaqueOnlyEffects = m_CameraPostProcessLayer.HasOpaqueOnlyEffects(m_PostProcessRenderContext);
            }
            
            //************************** Opaque Post-processing ************************************
            //Ambient Occlusion, Screen-spaced reflection are generally not supported for SRP
            //So this part is only for custom opaque post-processing
            // if(usePostProcessing)
            // {
            //     CommandBuffer cmdpp = new CommandBuffer();
            //     cmdpp.name = "("+camera.name+")"+ "Post-processing Opaque";

            //     m_PostProcessRenderContext.Reset();
            //     m_PostProcessRenderContext.camera = camera;
            //     m_PostProcessRenderContext.source = m_ColorRT;
            //     m_PostProcessRenderContext.sourceFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetRenderTextureFormat(m_ColorFormatActive);
            //     m_PostProcessRenderContext.destination = m_ColorRT;
            //     m_PostProcessRenderContext.command = cmdpp;
            //     m_PostProcessRenderContext.flip = camera.targetTexture == null;
            //     m_CameraPostProcessLayer.RenderOpaqueOnly(m_PostProcessRenderContext);
               
            //     context.ExecuteCommandBuffer(cmdpp);
            //     cmdpp.Release();
            // }

            //************************** Rendering Transparent Objects ************************************

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //************************** Transparent Post-processing ************************************
            //Bloom, Vignette, Grain, ColorGrading, LensDistortion, Chromatic Aberration, Auto Exposure, Motion Blur
            if(usePostProcessing)
            {
                CommandBuffer cmdpp = new CommandBuffer();
                cmdpp.name = "("+camera.name+")"+ "Post-processing Transparent";

                m_PostProcessRenderContext.Reset();
                m_PostProcessRenderContext.camera = camera;
                m_PostProcessRenderContext.source = m_ColorRT;
                m_PostProcessRenderContext.sourceFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetRenderTextureFormat(m_ColorFormatActive);
                m_PostProcessRenderContext.destination = BuiltinRenderTextureType.CameraTarget;
                m_PostProcessRenderContext.command = cmdpp;
                m_PostProcessRenderContext.flip = camera.targetTexture == null;
                m_CameraPostProcessLayer.Render(m_PostProcessRenderContext);
                
                context.ExecuteCommandBuffer(cmdpp);
                cmdpp.Release();
            }
            else
            {
                //Make sure screen has the thing when Postprocessing is off
                CommandBuffer cmdBlitToCam = new CommandBuffer();
                cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
                cmdBlitToCam.Blit(m_ColorRTid, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmdBlitToCam);
                cmdBlitToCam.Release();
            }

            //************************** Debug ************************************

            if(_motionVectorDebug)
            {
                CommandBuffer cmdDebug = new CommandBuffer();
                cmdDebug.Blit(BuiltinRenderTextureType.CameraTarget,BuiltinRenderTextureType.CameraTarget,motionVectorDebugMaterial);
                context.ExecuteCommandBuffer(cmdDebug);
                cmdDebug.Release();
            }

            //************************** Clean Up ************************************

            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_ColorRTid);
            cmdclean.ReleaseTemporaryRT(m_DepthRTid);
            cmdclean.ReleaseTemporaryRT(m_MotionVectorRTid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();

            //For camera motion vector
            _PreviousVP = _NonJitteredVP;

            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}