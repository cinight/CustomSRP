using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0802 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0802", priority = 1)]
    static void CreateSRP0802()
    {
        var instance = ScriptableObject.CreateInstance<SRP0802>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0802.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0802Instance();
    }
}

public class SRP0802Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802
    private static readonly ShaderTagId m_PassName2 = new ShaderTagId("SRP0802_Pass2"); //The shader pass tag just for SRP0802

    AttachmentDescriptor m_Albedo;
    AttachmentDescriptor m_Emission;
    AttachmentDescriptor m_Output;
    AttachmentDescriptor m_Depth;

    private static UnityEngine.Experimental.Rendering.GraphicsFormat m_ColorFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

    public SRP0802Instance()
    {
        m_Albedo = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
        m_Emission = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
        m_Output = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
        m_Depth = new AttachmentDescriptor(RenderTextureFormat.Depth);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(camera);

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

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings1 = new DrawingSettings(m_PassName1, sortingSettings);
            DrawingSettings drawSettings2 = new DrawingSettings(m_PassName2, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Camera clear flag
            // CommandBuffer cmd = new CommandBuffer();
            // cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            // context.ExecuteCommandBuffer(cmd);
            // cmd.Release();

            // Camera clear flag
            m_Albedo.ConfigureClear(camera.backgroundColor,1,0);
            m_Emission.ConfigureClear(Color.black,1,0);
            m_Depth.ConfigureClear(Color.black,1,0);
            m_Output.ConfigureTarget(BuiltinRenderTextureType.CameraTarget, false, true);
            //if(clearColor)

            /* 

            // RenderPass
            using (RenderPass rp = new RenderPass(context, camera.pixelWidth, camera.pixelHeight, 1, new[] { m_Albedo, m_Emission, m_Output }, m_Depth))
            {
                using (new RenderPass.SubPass(rp, new[] { m_Albedo, m_Emission }, null))
                {
                    // var settings = new DrawRendererSettings(camera, new ShaderPassName("BasicPass"))
                    // {
                    //     sorting = { flags = SortFlags.CommonOpaque }
                    // };
                    //var fs = new FilterRenderersSettings(true);
                    //fs.renderQueueRange = RenderQueueRange.opaque;

                    //Skybox
                    if(drawSkyBox)  {  context.DrawSkybox(camera);  }

                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings1, ref filterSettings);
                }
                
                using (new RenderPass.SubPass(rp, new[] { m_Output }, new[] { m_Albedo, m_Emission }, false))
                {
                    context.DrawSkybox(camera);
                    // var settings = new DrawRendererSettings(camera, new ShaderPassName("AddPass"))
                    // {
                    //     sorting = { flags = SortFlags.CommonOpaque }
                    // };
                    // var fs = new FilterRenderersSettings(true);
                    // fs.renderQueueRange = RenderQueueRange.opaque;
                    // context.DrawRenderers(cull.visibleRenderers, ref settings, fs);

                    //Opaque objects
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);

                    //Transparent objects
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings2, ref filterSettings);
                }
            }

            */





            context.Submit();
        }
    }
}