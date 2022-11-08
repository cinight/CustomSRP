using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class CustomSRPUtil
{
    public static void RenderSkybox(ScriptableRenderContext context, Camera camera)
    {
        RendererList rl = context.CreateSkyboxRendererList(camera);
        CommandBuffer cmdSkybox = new CommandBuffer();
        cmdSkybox.name = "Render Skybox";
        cmdSkybox.DrawRendererList(rl);
        context.ExecuteCommandBuffer(cmdSkybox);
        cmdSkybox.Release();
    }

    public static void RenderObjects(string name, ScriptableRenderContext context, CullingResults cull, FilteringSettings filterSettings, DrawingSettings drawSettings)
    {
        RendererListParams rlp = new RendererListParams(cull, drawSettings, filterSettings);
        RendererList rl = context.CreateRendererList(ref rlp);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = name;
        cmd.DrawRendererList(rl);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }

}
