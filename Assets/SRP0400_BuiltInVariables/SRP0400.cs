using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[ExecuteInEditMode]
public class SRP0400 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0400", priority = 1)]
    static void CreateSRP0400()
    {
        var instance = ScriptableObject.CreateInstance<SRP0400>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0400.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0400Instance();
    }
}

public class SRP0400Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0400_Pass"); //The shader pass tag just for SRP0400

    static int lightColorID = Shader.PropertyToID("_LightColor");
    static int lightDataID = Shader.PropertyToID("_LightData");

    public SRP0400Instance()
    {
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

            //SetUp Lighting variables
            SetUpRealtimeLightingVariables(context,cull);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Camera clear flag
            var cmd = CommandBufferPool.Get("Clear");
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

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

            context.Submit();
        }
    }

    private static int lightCount = 8;      
    Vector4[] lightColor = new Vector4[lightCount];
    Vector4[] lightData = new Vector4[lightCount];

    private void SetUpRealtimeLightingVariables(ScriptableRenderContext context, CullingResults cull)
    {
        int lightCount = 8;//Mathf.Min(cull.visibleLights.Length,8); //Max 8 lights
        int actualLightCount = cull.visibleLights.Length;
        //if(lightCount == 0) return;

        // for (var i = 0; i < lightCount; i++)
        // {
        //     lightColor[i] = Vector4.zero;
        //     lightData[i] = Vector4.zero;
        // }


        // for (var i = 0; i < lightCount; i++)
        // {
        //     lightColor[i] = Vector4.zero;
        //     lightData[i] = Vector4.zero;

        //     if( i >= actualLightCount) continue;



        //     VisibleLight light = cull.visibleLights[i];
        //     Vector4 data = Vector4.zero;
            
        //     if (light.lightType == LightType.Directional)
        //     {
        //         // If it's a directional light we store direction in the xyz components, and a negative
        //         // value in the w component. This allows us to identify whether it is a directional light.
        //         data = light.localToWorldMatrix.MultiplyVector(Vector3.back);
        //         data.w = -1;
        //     }
        //     else if (light.lightType == LightType.Point)
        //     {
        //         // If it's a point light we store position in the xyz components, 
        //         // and range in the w component.
        //         data = light.localToWorldMatrix.GetColumn(3);
        //         data.w = light.range;
        //     }
        //     else if (light.lightType == LightType.Spot)
        //     {
        //         // If it's a spot light we store direction in the xyz components
        //         data = light.localToWorldMatrix.GetColumn(2);
        //         data.x = -data.x;
        //         data.y = -data.y;
        //         data.z = -data.z;
        //         data.w = 0;
        //     }
        //     else
        //     {
        //         // If it's not a point / directional / spot light, we ignore the light.
        //         continue;
        //     }

        //     lightColor[i] = light.finalColor;
        //     lightData[i] = data;
        // }
        
        // CommandBuffer cmdLight = CommandBufferPool.Get("Set-up Light Buffer");
        // cmdLight.SetGlobalVectorArray(lightColorID, lightColor);
        // cmdLight.SetGlobalVectorArray(lightDataID, lightData);
        // context.ExecuteCommandBuffer(cmdLight);
        // CommandBufferPool.Release(cmdLight);
    }
}