using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

//https://github.com/Unity-Technologies/Graphics/blob/10.x.x/release/com.unity.render-pipelines.core/Tests/Editor/RenderGraphTests.cs

// PIPELINE MAIN --------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraph : RenderPipeline
{
    private RenderGraph graph = new RenderGraph("SRP0802_RenderGraphPass");
    //private int m_ColorRTid = Shader.PropertyToID("_CameraColorTexture");
    //private RenderTargetIdentifier m_ColorRT;

    public SRP0802_RenderGraph()
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
            if (!camera.TryGetCullingParameters(out cullingParams)) continue;
            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Execute RenderGraph

            SRP0802_BasePassData basePassData = Render_SRP0802_BasePass(camera,graph,cull);
            Render_SRP0802_AddPass(camera,graph,cull,basePassData.m_Albedo,basePassData.m_Emission);

            CommandBuffer cmdRG = CommandBufferPool.Get("ExecuteRenderGraph");
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                commandBuffer = cmdRG,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            graph.Begin(rgParams);
            graph.Execute();

            context.ExecuteCommandBuffer(cmdRG);
            context.Submit();

            CommandBufferPool.Release(cmdRG);

            EndCameraRendering(context,camera);
        }
        
        graph.EndFrame();
        EndFrameRendering(context,cameras);
    }

    protected override void Dispose(bool disposing)
    {
        graph.Cleanup();
        graph = null;
    }
}