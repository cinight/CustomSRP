using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP1001 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP1001", priority = 1)]
    static void CreateSRP1001()
    {
        var instance = ScriptableObject.CreateInstance<SRP1001>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP1001.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP1001Instance();
    }
}

public class SRP1001Instance : RenderPipeline
{
    private static Material errorMaterial;
    private static List<ShaderTagId> m_LegacyShaderPassNames;

    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP1001_Pass"); //The shader pass tag just for SRP1001

    public SRP1001Instance()
    {
        errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        m_LegacyShaderPassNames = new List<ShaderTagId>()
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };
    }

    private void DrawErrorShader(ScriptableRenderContext context, SortingSettings sortingSettings, CullingResults cull, FilteringSettings filterSettings)
    {
        //Replace all legacy pass tag shaders by the pink error shader
        if (errorMaterial != null)
        {
            sortingSettings.criteria = SortingCriteria.None;
            filterSettings.renderQueueRange = RenderQueueRange.all;
            DrawingSettings errorSettings = new DrawingSettings(m_LegacyShaderPassNames[0], sortingSettings)
            {
                perObjectData = PerObjectData.None,
                overrideMaterial = errorMaterial,
                overrideMaterialPassIndex = 0
            };
            for (int i = 1; i < m_LegacyShaderPassNames.Count; ++i)
            {
                errorSettings.SetShaderPassName(i, m_LegacyShaderPassNames[i]);
            }
            context.DrawRenderers(cull, ref errorSettings, ref filterSettings);
        }
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

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Error Opaque
            //DrawErrorShader(context,sortingSettings,cull,filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Error Opaque+Transparent
            DrawErrorShader(context,sortingSettings,cull,filterSettings);

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}