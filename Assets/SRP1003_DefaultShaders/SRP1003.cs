using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP1003 : RenderPipelineAsset<SRP1003Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP1003", priority = 1)]
    static void CreateSRP1003()
    {
        var instance = ScriptableObject.CreateInstance<SRP1003>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP1003.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP1003Instance();
    }


    #if UNITY_EDITOR
    //==================== Default Materials =======================
    public override Material defaultMaterial
    {
        get { return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SRP1003_DefaultShaders/SRP1003_UnlitOpaque.mat"); }
    }

    public override Material defaultParticleMaterial
    {
        get { return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SRP1003_DefaultShaders/SRP1003_UnlitTransparent.mat"); }
    }

    public override Material defaultLineMaterial
    {
        get { return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SRP1003_DefaultShaders/SRP1003_UnlitTransparent.mat"); }
    }

    public override Material defaultTerrainMaterial
    {
        get { return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SRP1003_DefaultShaders/SRP1003_UnlitOpaque.mat"); }
    }

    public override Material defaultUIMaterial
    {
        get { return null; }
    }

    public override Material defaultUIOverdrawMaterial
    {
        get { return null; }
    }

    public override Material defaultUIETC1SupportedMaterial
    {
        get { return null; }
    }

    public override Material default2DMaterial
    {
        get { return null; }
    }

    //==================== Default Shaders =======================

    public override Shader defaultShader
    {
        get { return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/SRP1003_DefaultShaders/SRP1003_UnlitOpaque.shader"); }
    }

    // #if UNITY_EDITOR
    // public override Shader autodeskInteractiveShader
    // {
    //     get { return editorResources.autodeskInteractiveShader; }
    // }

    // public override Shader autodeskInteractiveTransparentShader
    // {
    //     get { return editorResources.autodeskInteractiveTransparentShader; }
    // }

    // public override Shader autodeskInteractiveMaskedShader
    // {
    //     get { return editorResources.autodeskInteractiveMaskedShader; }
    // }

    // public override Shader terrainDetailLitShader
    // {
    //     get { return editorResources.terrainDetailLitShader; }
    // }

    // public override Shader terrainDetailGrassShader
    // {
    //     get { return editorResources.terrainDetailGrassShader; }
    // }

    // public override Shader terrainDetailGrassBillboardShader
    // {
    //     get { return editorResources.terrainDetailGrassBillboardShader; }
    // }
    // #endif
    #endif

}

public class SRP1003Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP1003_Pass"); //The shader pass tag just for SRP1003

    public SRP1003Instance()
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

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)
            {
                CustomSRPUtil.RenderSkybox(context, camera);
            }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Opaque Objects", context, cull, filterSettings, drawSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }
}