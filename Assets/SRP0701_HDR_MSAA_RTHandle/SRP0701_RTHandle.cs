//RTHandle documentation - https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@14.0/manual/rthandle-system-using.html
//URP upgrade guide has some explanations about RTHandle - https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@13.1/manual/upgrade-guide-2022-1.html
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0701_RTHandle : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0701_RTHandle", priority = 1)]
    static void CreateSRP0701_RTHandle()
    {
        var instance = ScriptableObject.CreateInstance<SRP0701_RTHandle>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0701_RTHandle.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0701_RTHandleInstance();
    }
}

public class SRP0701_RTHandleInstance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0701_Pass"); //We are reusing the shaders in SRP0701
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatHDR = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    //private DepthBits depth = DepthBits.Depth16; //16 won't have stencil

    public SRP0701_RTHandleInstance()
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
            ClearFlag clear = ClearFlag.All;
            switch(camera.clearFlags)
            {
                case CameraClearFlags.Skybox: clear = ClearFlag.Depth; break;
                case CameraClearFlags.Nothing: clear = ClearFlag.None; break;
                case CameraClearFlags.Color: clear = ClearFlag.All; break;
            }
           
            UnityEngine.Experimental.Rendering.GraphicsFormat format = camera.allowHDR ? m_ColorFormatHDR : m_ColorFormat;
            MSAASamples msaa = MSAASamples.None;
            if(camera.allowMSAA)
            {
                switch( QualitySettings.antiAliasing )
                {
                    case 2: msaa = MSAASamples.MSAA2x; break;
                    case 4: msaa = MSAASamples.MSAA4x; break;
                    case 8: msaa = MSAASamples.MSAA8x; break;
                }
            }

            //Setup RTHandle
            RTHandles.Initialize(Screen.width, Screen.height);
            RTHandles.SetReferenceSize(camera.pixelWidth, camera.pixelHeight);
            RTHandle m_ColorRT = RTHandles.Alloc(scaleFactor: Vector2.one, colorFormat: format, depthBufferBits: DepthBits.None, msaaSamples: msaa, name: "_CameraColorRT");

            //Set RenderTarget & Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "SetRT and Clear";
            CoreUtils.SetRenderTarget(cmd,m_ColorRT,clear,camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(camera.clearFlags == CameraClearFlags.Skybox)  {  context.DrawSkybox(camera);  }

            //RendererList Opaque
            UnityEngine.Rendering.RendererUtils.RendererListDesc rendererDesc_Opaque = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName,cull,camera);
            rendererDesc_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererDesc_Opaque.renderQueueRange = RenderQueueRange.opaque;
            UnityEngine.Rendering.RendererList rdlist_Opaque = context.CreateRendererList(rendererDesc_Opaque);

            //RendererList Transparent
            UnityEngine.Rendering.RendererUtils.RendererListDesc rendererDesc_Transparent = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName,cull,camera);
            rendererDesc_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
            rendererDesc_Transparent.renderQueueRange = RenderQueueRange.transparent;
            UnityEngine.Rendering.RendererList rdlist_Transparent = context.CreateRendererList(rendererDesc_Transparent);

            //Draw RendererLists
            CommandBuffer cmdRender = new CommandBuffer();
            cmdRender.name = "Draw RendererLists";
            CoreUtils.DrawRendererList(context,cmdRender,rdlist_Opaque);
            CoreUtils.DrawRendererList(context,cmdRender,rdlist_Transparent);
            context.ExecuteCommandBuffer(cmdRender);
            cmdRender.Release();

            //Blit the content back to screen
            CommandBuffer cmdBlitToCam = new CommandBuffer();
            cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
            cmdBlitToCam.Blit(m_ColorRT, BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmdBlitToCam);
            cmdBlitToCam.Release();
            
            context.Submit();

            EndCameraRendering(context,camera);

            //Cleanup
            RTHandles.Release(m_ColorRT);
        }

        EndFrameRendering(context,cameras);
    }
}