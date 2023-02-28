using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0802_RenderGraph_RasterCommandBufferAsset : RenderPipelineAsset<SRP0802_RenderGraph_RasterCommandBuffer>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802_RenderGraph_RasterCommandBuffer", priority = 1)]
    static void CreateSRP0802_RenderGraph_RasterCommandBuffer()
    {
        var instance = ScriptableObject.CreateInstance<SRP0802_RenderGraph_RasterCommandBufferAsset>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802_RenderGraph_RasterCommandBuffer.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802_RenderGraph_RasterCommandBuffer();
    }
}