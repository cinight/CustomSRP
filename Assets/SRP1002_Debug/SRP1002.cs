using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP1002 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP1002", priority = 1)]
    static void CreateSRP1002()
    {
        var instance = ScriptableObject.CreateInstance<SRP1002>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP1002.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP1002Instance();
    }
}

public class SRP1002Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP1002_Pass"); //The shader pass tag just for SRP1002

    private static int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    private static RenderTargetIdentifier m_ColorRT = new RenderTargetIdentifier(m_ColorRTid);
    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

    private int depthBufferBits = 24;

    private string debugTag = "";

    public SRP1002Instance()
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

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //cmd.BeginSample("xxx");
            //cmd.EndSample("xxx");
            //UnityEngine.Profiling.Profiler.BeginSample("xxx");
            //UnityEngine.Profiling.Profiler.EndSample();

            //Camera clear flag
            {
                debugTag = "Debug - ClearRenderTarget";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    cmd.GetTemporaryRT(m_ColorRTid, colorRTDesc,FilterMode.Bilinear);
                    cmd.SetRenderTarget(m_ColorRT);
                    cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            // Skybox
            {
                debugTag = "Debug - DrawSkyBox";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    if(drawSkyBox)  {  context.DrawSkybox(camera);  }
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Opaque objects
            {
                debugTag = "Debug - DrawOpaqueObjects";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings, ref filterSettings);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Transparent objects
            {
                debugTag = "Debug - DrawTransparentObjects";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings, ref filterSettings);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Blit to screen
            {
                debugTag = "Debug - BlitToScreen";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    cmd.Blit(m_ColorRT,BuiltinRenderTextureType.CameraTarget);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Clean Up
            {
                debugTag = "Debug - CleanUp";
                CommandBuffer cmd = CommandBufferPool.Get(debugTag);
                using (new ProfilingSample(cmd, debugTag))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    cmd.ReleaseTemporaryRT(m_ColorRTid);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            context.Submit();
        }
    }
}