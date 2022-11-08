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

    private ProfilingSampler sampler_ClearRenderTarget;
    private ProfilingSampler sampler_DrawSkyBox;
    private ProfilingSampler sampler_DrawOpaqueObjects;
    private ProfilingSampler sampler_DrawTransparentObjects;
    private ProfilingSampler sampler_BlitToScreen;
    private ProfilingSampler sampler_CleanUp;

    public SRP1002Instance()
    {
        sampler_ClearRenderTarget = new ProfilingSampler ("Debug - ClearRenderTarget");
        sampler_DrawSkyBox = new ProfilingSampler ("Debug - DrawSkyBox");
        sampler_DrawOpaqueObjects = new ProfilingSampler ("Debug - DrawOpaqueObject");
        sampler_DrawTransparentObjects = new ProfilingSampler ("Debug - DrawTransparentObjects");
        sampler_BlitToScreen = new ProfilingSampler ("Debug - sampler_ClearRenderTarget");
        sampler_CleanUp = new ProfilingSampler ("Debug - CleanUp");
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
                CommandBuffer cmd = CommandBufferPool.Get(sampler_ClearRenderTarget.name);
                using (new ProfilingScope(cmd, sampler_ClearRenderTarget))
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
                CommandBuffer cmd = CommandBufferPool.Get(sampler_DrawSkyBox.name);
                using (new ProfilingScope(cmd, sampler_DrawSkyBox))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    if(drawSkyBox)
                    {
                        CustomSRPUtil.RenderSkybox(context, camera);
                    }
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Opaque objects
            {
                CommandBuffer cmd = CommandBufferPool.Get(sampler_DrawOpaqueObjects.name);
                using (new ProfilingScope(cmd, sampler_DrawOpaqueObjects))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    CustomSRPUtil.RenderObjects("Render Opaque Objects", context, cull, filterSettings, drawSettings);

                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Transparent objects
            {
                CommandBuffer cmd = CommandBufferPool.Get(sampler_DrawTransparentObjects.name);
                using (new ProfilingScope(cmd, sampler_DrawTransparentObjects))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);
                    //
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            //Blit to screen
            {
                CommandBuffer cmd = CommandBufferPool.Get(sampler_BlitToScreen.name);
                using (new ProfilingScope(cmd, sampler_BlitToScreen))
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
                CommandBuffer cmd = CommandBufferPool.Get(sampler_CleanUp.name);
                using (new ProfilingScope(cmd, sampler_CleanUp))
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
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}