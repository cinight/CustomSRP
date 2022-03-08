//RTHandle documentation - https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@14.0/manual/rthandle-system-using.html
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
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0701_RTHandle_Pass"); //The shader pass tag just for SRP0701_RTHandle
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormatHDR = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_DepthFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None; //RenderTextureFormat.Depth
    private DepthBits depthBufferBits = DepthBits.Depth24; //16 won't have stencil

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
                case CameraClearFlags.Color: clear = ClearFlag.Color; break;
            }

            //************************** Start Set TempRT ************************************
            
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

            RTHandles.Initialize(Screen.width, Screen.height);
            RTHandles.SetReferenceSize(camera.pixelWidth, camera.pixelHeight);
            RTHandle m_ColorRT = RTHandles.Alloc(scaleFactor: Vector2.one, colorFormat: format, depthBufferBits: depthBufferBits, msaaSamples: msaa, name: "_CameraColorRT");
            RTHandle m_DepthRT = RTHandles.Alloc(scaleFactor: Vector2.one, colorFormat: m_DepthFormat, depthBufferBits: depthBufferBits, name: "_CameraDepthRT");

            //************************** End Set TempRT ************************************

            //Set RenderTarget & Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            CoreUtils.SetRenderTarget(cmd,m_ColorRT,m_DepthRT,clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(camera.clearFlags == CameraClearFlags.Skybox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Blit the content back to screen
            CommandBuffer cmdBlitToCam = new CommandBuffer();
            cmdBlitToCam.name = "("+camera.name+")"+ "Blit back to Camera";
            cmdBlitToCam.Blit(m_ColorRT, BuiltinRenderTextureType.CameraTarget);
            context.ExecuteCommandBuffer(cmdBlitToCam);
            cmdBlitToCam.Release();
            
            //Cleanup
            RTHandles.Release(m_ColorRT);
            RTHandles.Release(m_DepthRT);

            context.Submit();

            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }

    //************************** Clean Up ************************************
    protected override void Dispose(bool disposing)
    {
        
    }
}