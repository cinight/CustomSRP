using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[ExecuteInEditMode]
public class SRP0603 : RenderPipelineAsset<SRP0603Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0603", priority = 1)]
    static void CreateSRP0603()
    {
        var instance = ScriptableObject.CreateInstance<SRP0603>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0603.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0603Instance();
    }
}

public class SRP0603Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0603_Pass"); //The shader pass tag just for SRP0603
    private static readonly ShaderTagId m_passNameShadow = new ShaderTagId("ShadowCaster"); //For shadow

    private static int m_DepthRTid = Shader.PropertyToID("_CameraDepthTexture"); //depth
    private static int m_ShadowMapLightid = Shader.PropertyToID("_ShadowMap"); //light pov depth
    private static int m_ShadowMapid = Shader.PropertyToID("_ShadowMapTexture"); //Use in shader, for screen-space shadow

    private static RenderTargetIdentifier m_DepthRT = new RenderTargetIdentifier(m_DepthRTid);
    private static RenderTargetIdentifier m_ShadowMapLight = new RenderTargetIdentifier(m_ShadowMapLightid);
    private static RenderTargetIdentifier m_ShadowMap = new RenderTargetIdentifier(m_ShadowMapid);

    public static Material m_ScreenSpaceShadowsMaterial;

    private static int depthBufferBits = 24;
    private static int m_ShadowRes = 2048;
    private static float m_shadowDistance = 50; //QualitySettings.shadowDistance

    //Realtime Lights
    static int lightColorID = Shader.PropertyToID("_LightColorArray");
    static int lightDataID = Shader.PropertyToID("_LightDataArray");
    static int lightSpotDirID = Shader.PropertyToID("_LightSpotDirArray");
    private const int lightCount = 16;      
    Vector4[] lightColor = new Vector4[lightCount];
    Vector4[] lightData = new Vector4[lightCount];
    Vector4[] lightSpotDir = new Vector4[lightCount];

    public SRP0603Instance()
    {
        m_ScreenSpaceShadowsMaterial = new Material(Shader.Find("Hidden/My/ScreenSpaceShadows"));
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
            cullingParams.shadowDistance = Mathf.Min( m_shadowDistance , camera.farClipPlane); // shadow distance
            CullingResults cull = context.Cull(ref cullingParams);

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings)
            {
                perObjectData = PerObjectData.LightIndices | PerObjectData.LightData
            };
            DrawingSettings drawSettingsDepth = new DrawingSettings(m_passNameShadow, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Set temp RT
            CommandBuffer cmdTempId = new CommandBuffer();
            cmdTempId.name = "("+camera.name+")"+ "Setup TempRT";
                //Depth
                RenderTextureDescriptor depthRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                depthRTDesc.colorFormat = RenderTextureFormat.Depth;
                depthRTDesc.depthBufferBits = depthBufferBits;
                cmdTempId.GetTemporaryRT(m_DepthRTid, depthRTDesc,FilterMode.Bilinear);
                //Shadow
                RenderTextureDescriptor shadowRTDesc = new RenderTextureDescriptor(m_ShadowRes,m_ShadowRes);
                shadowRTDesc.colorFormat = RenderTextureFormat.Shadowmap;
                shadowRTDesc.depthBufferBits = depthBufferBits; //have depth because it is also a depth texture
                cmdTempId.GetTemporaryRT(m_ShadowMapLightid, shadowRTDesc,FilterMode.Bilinear);//depth per light
                //ScreenSpaceShadowMap
                RenderTextureDescriptor shadowMapRTDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                shadowMapRTDesc.colorFormat = RenderTextureFormat.Default;
                shadowMapRTDesc.depthBufferBits = 0;
                cmdTempId.GetTemporaryRT(m_ShadowMapid, shadowMapRTDesc, FilterMode.Bilinear);//screen space shadow
            context.ExecuteCommandBuffer(cmdTempId);
            cmdTempId.Release();

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Clear ScreenSpaceShadowMap Texture
            CommandBuffer cmdSSSMclear = new CommandBuffer();
            cmdSSSMclear.name = "("+camera.name+")"+ "Clear ScreenSpaceShadowMap";
            cmdSSSMclear.SetRenderTarget(m_ShadowMap); //Set CameraTarget to the depth texture
            cmdSSSMclear.ClearRenderTarget(false, true, Color.white);
            context.ExecuteCommandBuffer(cmdSSSMclear);
            cmdSSSMclear.Release();

            //Clear Depth Texture
            CommandBuffer cmdDepth = new CommandBuffer();
            cmdDepth.name = "("+camera.name+")"+ "Depth Clear Flag";
            cmdDepth.SetRenderTarget(m_DepthRT); //Set CameraTarget to the depth texture
            cmdDepth.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmdDepth);
            cmdDepth.Release();

            //Draw Depth with Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettingsDepth.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            CustomSRPUtil.RenderObjects("Render Opaque Objects Depth", context, cull, filterSettings, drawSettingsDepth);

            //To let shader has _CameraDepthTexture
            CommandBuffer cmdDepthTexture = new CommandBuffer();
            cmdDepthTexture.name = "("+camera.name+")"+ "Depth Texture";
            cmdDepthTexture.SetGlobalTexture(m_DepthRTid,m_DepthRT);
            context.ExecuteCommandBuffer(cmdDepthTexture);
            cmdDepthTexture.Release();

            //SetUp Lighting & shadow variables
            SetUpRealtimeLightingVariables(camera, context,cull);

            //Debug **********************************************
            // CommandBuffer cmdDebug = new CommandBuffer();
            // cmdDebug.name = "Debug";
            // cmdDebug.Blit( m_ShadowMap, BuiltinRenderTextureType.CameraTarget );
            // context.ExecuteCommandBuffer(cmdDebug);
            // cmdDebug.Release();

            // Color Rendering============================================================================

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Camera clear flag
            var cmd = CommandBufferPool.Get("Clear");
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

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

            //Clean Up
            CommandBuffer cmdclean = new CommandBuffer();
            cmdclean.name = "("+camera.name+")"+ "Clean Up";
            cmdclean.ReleaseTemporaryRT(m_DepthRTid);
            cmdclean.ReleaseTemporaryRT(m_ShadowMapLightid);
            cmdclean.ReleaseTemporaryRT(m_ShadowMapid);
            context.ExecuteCommandBuffer(cmdclean);
            cmdclean.Release();

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }

    private void SetUpRealtimeShadowVariables(Camera cam, ScriptableRenderContext context, CullingResults cull, Light light, int lightIndex)
    {
        Bounds bounds;
        bool doShadow = light.shadows != LightShadows.None && cull.GetShadowCasterBounds(lightIndex, out bounds);

        //************************** Shadow Mapping ************************************
        if (doShadow)
        {
            //For shadowmapping, the matrices from the light's point of view
            Matrix4x4 view = Matrix4x4.identity;
            Matrix4x4 proj = Matrix4x4.identity;
            ShadowSplitData splitData;

            bool successShadowMap = false;
            if (light.type == LightType.Directional)
            {
                successShadowMap = cull.ComputeDirectionalShadowMatricesAndCullingPrimitives
                (
                    lightIndex,
                    0, 1, new Vector3(1,0,0),
                    m_ShadowRes, light.shadowNearPlane, out view, out proj, out splitData
                );

                BatchCullingProjectionType projType = cam.orthographic? BatchCullingProjectionType.Orthographic : BatchCullingProjectionType.Perspective;
                ShadowCastersCullingInfos infos = new ShadowCastersCullingInfos();
                infos.perLightInfos = new NativeArray<LightShadowCasterCullingInfo>(1,Allocator.Temp);
                infos.perLightInfos[lightIndex] = new LightShadowCasterCullingInfo()
                {
                    splitRange = new RangeInt(0,1), //0 offset and 1 slice
                    projectionType = projType
                };
                infos.splitBuffer = new NativeArray<ShadowSplitData>(1,Allocator.Temp);
                infos.splitBuffer[lightIndex] = splitData;
                context.CullShadowCasters(cull,infos);
            }
            else return;

            if(successShadowMap)
            {
                CommandBuffer cmdShadow = new CommandBuffer();
                cmdShadow.name = "Shadow Mapping: light"+lightIndex;
                cmdShadow.SetRenderTarget(m_ShadowMapLight);
                cmdShadow.ClearRenderTarget(true, true, Color.black);
                //Change the view to light's point of view
                cmdShadow.SetViewport(new Rect(0, 0, m_ShadowRes, m_ShadowRes));
                cmdShadow.EnableScissorRect(new Rect(4, 4, m_ShadowRes - 8, m_ShadowRes - 8));
                cmdShadow.SetViewProjectionMatrices(view, proj);
                context.ExecuteCommandBuffer(cmdShadow);
                cmdShadow.Clear();

                //Render Shadowmap
                ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(cull, lightIndex);
                RendererList rl_shadow = context.CreateShadowRendererList(ref shadowSettings);
                CommandBuffer cmd_shadow = new CommandBuffer();
                cmd_shadow.name = "Render Shadowmap";
                cmd_shadow.DrawRendererList(rl_shadow);
                context.ExecuteCommandBuffer(cmd_shadow);
                cmd_shadow.Release();
                
                //Set shadowmap texture
                cmdShadow.DisableScissorRect();
                cmdShadow.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
                cmdShadow.SetGlobalTexture(m_ShadowMapLightid, m_ShadowMapLight);
                context.ExecuteCommandBuffer(cmdShadow);
                cmdShadow.Clear();
                cmdShadow.Release();

                //Screen Space Shadow =================================================
                CommandBuffer cmdShadow2 = new CommandBuffer();
                cmdShadow2.name = "Screen Space Shadow: light"+lightIndex;

                //Bias
                float sign = (SystemInfo.usesReversedZBuffer) ? 1.0f : -1.0f;
                float bias = light.shadowBias * proj.m22 * sign;
                cmdShadow2.SetGlobalFloat("_ShadowBias", bias);

                //Shadow Transform                
                if (SystemInfo.usesReversedZBuffer)
                {
                    proj.m20 = -proj.m20;
                    proj.m21 = -proj.m21;
                    proj.m22 = -proj.m22;
                    proj.m23 = -proj.m23;
                }
                
                Matrix4x4 WorldToShadow = proj * view;

                float f = 0.5f;

                var textureScaleAndBias = Matrix4x4.identity;
                textureScaleAndBias.m00 = f;
                textureScaleAndBias.m11 = f;
                textureScaleAndBias.m22 = f;
                textureScaleAndBias.m03 = f;
                textureScaleAndBias.m23 = f;
                textureScaleAndBias.m13 = f;

                WorldToShadow = textureScaleAndBias * WorldToShadow;

                cmdShadow2.SetGlobalMatrix("_WorldToShadow", WorldToShadow);
                cmdShadow2.SetGlobalFloat("_ShadowStrength", light.shadowStrength);

                //Render the screen-space shadow
                cmdShadow2.Blit(m_ShadowMap, m_ShadowMap, m_ScreenSpaceShadowsMaterial);
                cmdShadow2.SetGlobalTexture(m_ShadowMapid,m_ShadowMap);
                context.ExecuteCommandBuffer(cmdShadow2);
                cmdShadow2.Release();
            }
        }
    }

    private void SetUpRealtimeLightingVariables(Camera cam, ScriptableRenderContext context, CullingResults cull)
    {
        for (var i = 0; i < lightCount; i++)
        {
            lightColor[i] = Vector4.zero;
            lightData[i] = Vector4.zero;
            lightSpotDir[i] = Vector4.zero;

            if( i >= cull.visibleLights.Length ) continue;
            VisibleLight light = cull.visibleLights[i];
            
            if (light.lightType == LightType.Directional)
            {
                lightData[i] = light.localToWorldMatrix.MultiplyVector(Vector3.back);
                lightColor[i] = light.finalColor;
                lightColor[i].w = -1; //for identifying it is a directional light in shader

                SetUpRealtimeShadowVariables(cam, context, cull, light.light, i); //setup shadow
                
            }
            else if (light.lightType == LightType.Point)
            {
                lightData[i] = light.localToWorldMatrix.GetColumn(3);
                lightData[i].w = light.range;
                lightColor[i] = light.finalColor;
                lightColor[i].w = -2; //for identifying it is a point light in shader
            }
            else if (light.lightType == LightType.Spot)
            {
                lightData[i] = light.localToWorldMatrix.GetColumn(3);
                lightData[i].w = 1f /Mathf.Max(light.range * light.range, 0.00001f);

                lightSpotDir[i] = light.localToWorldMatrix.GetColumn(2);
                lightSpotDir[i].x = -lightSpotDir[i].x;
                lightSpotDir[i].y = -lightSpotDir[i].y;
                lightSpotDir[i].z = -lightSpotDir[i].z;
                lightColor[i] = light.finalColor;

                float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
				float outerCos = Mathf.Cos(outerRad);
                float outerTan  = Mathf.Tan(outerRad);
				float innerCos = Mathf.Cos(Mathf.Atan(((46f / 64f) * outerTan)));
                float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);

                //Spotlight attenuation
                lightSpotDir[i].w = 1f / angleRange;
				lightColor[i].w = -outerCos * lightSpotDir[i].w;
            }
            else
            {
                // If it's not a point / directional / spot light, we ignore the light.
                continue;
            }
        }
        
        CommandBuffer cmdLight = CommandBufferPool.Get("Set-up Light Buffer");
        cmdLight.SetGlobalVectorArray(lightDataID, lightData);
        cmdLight.SetGlobalVectorArray(lightColorID, lightColor);
        cmdLight.SetGlobalVectorArray(lightSpotDirID, lightSpotDir);
        context.ExecuteCommandBuffer(cmdLight);
        CommandBufferPool.Release(cmdLight);
    }
}