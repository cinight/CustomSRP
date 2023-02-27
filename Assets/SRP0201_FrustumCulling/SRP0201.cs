using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0201 : RenderPipelineAsset<SRP0201Instance>
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0201", priority = 1)]
    static void CreateSRP0201()
    {
        var instance = ScriptableObject.CreateInstance<SRP0201>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0201.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0201Instance();
    }
}

public class SRP0201Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0201_Pass"); //The shader pass tag just for SRP0201
    private static readonly ShaderTagId m_PassNameDefault = new ShaderTagId("SRPDefaultUnlit"); //The shader pass tag for replacing shaders without pass

    //Scene Object Lists
    public static TextMesh textMesh;
    public static Renderer[] rens;
    public static Light[] lights;
    public static ReflectionProbe[] reflprobes;

    public SRP0201Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        foreach (Camera camera in cameras)
        {
            #if UNITY_EDITOR
            bool isSceneViewCam = camera.cameraType == CameraType.SceneView;
            if(isSceneViewCam) ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); //This makes the UI Canvas geometry appear on scene view
            #endif

            BeginCameraRendering(context,camera);

            //Culling
            ScriptableCullingParameters cullingParams;
            if (!camera.TryGetCullingParameters(out cullingParams))
                continue;
            CullingResults cull = context.Cull(ref cullingParams);

            ShowCullingResult(camera, cull); //*******************************

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
            DrawingSettings drawSettingsDefault = new DrawingSettings(m_PassNameDefault, sortingSettings);
                            drawSettingsDefault.SetShaderPassName(1,m_PassNameDefault); //This will let you draw shader passes without the LightMode
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

            //Opaque default
            drawSettingsDefault.sortingSettings = sortingSettings;
            CustomSRPUtil.RenderObjects("Render Opaque Objects Default Pass", context, cull, filterSettings, drawSettingsDefault);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            CustomSRPUtil.RenderObjects("Render Transparent Objects", context, cull, filterSettings, drawSettings);

            //Transparent default
            drawSettingsDefault.sortingSettings = sortingSettings;
            CustomSRPUtil.RenderObjects("Render Transparent Objects Default Pass", context, cull, filterSettings, drawSettingsDefault);

            //SceneView fix, so that it draws the gizmos on scene view
            #if UNITY_EDITOR
            if (isSceneViewCam)
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
            #endif

            context.Submit();
            
            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);
    }

    protected void ShowCullingResult(Camera camera, CullingResults cull)
    {
        if( camera.name == "MainCamera" || camera.name == "AllCam" || camera.name == "NoneCam" ) 
        {
            string tx = "";
            tx += "Culling Result of : " + camera.name + " \n";
            tx += "\n";

            //Visible Lights
            VisibleLight[] ls = cull.visibleLights.ToArray();
            tx += "Lights : Visible : "+ls.Length+"\n";
            
            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    int existed = 0;
                    for (int j = 0; j < ls.Length; j++)
                    {
                        if (lights[i] == ls[j].light)
                        {
                            existed++;
                        }
                    }
                    if (existed > 0)
                    {
                        tx += lights[i].gameObject.name + " : <color=#0F0>Visible</color>" + "\n";
                    }
                    else
                    {
                        tx += lights[i].gameObject.name + " : <color=#F00>Not Visible</color>" + "\n";
                    }
                }
            }
            else
            {
                tx += "Light list is null \n";
            }
            tx += "\n";

            //Visible Reflection Probes
            VisibleReflectionProbe[] rs = cull.visibleReflectionProbes.ToArray();
            tx += "Reflection Probes : Visible : "+rs.Length+"\n";
            
            if (reflprobes != null)
            {
                for (int i = 0; i < reflprobes.Length; i++)
                {
                    int existed = 0;
                    for (int j = 0; j < rs.Length; j++)
                    {
                        if (reflprobes[i] == rs[j].reflectionProbe)
                        {
                            existed++;
                        }
                    }
                    if (existed > 0)
                    {
                        tx += reflprobes[i].gameObject.name + " : <color=#0F0>Visible</color>" + "\n";
                    }
                    else
                    {
                        tx += reflprobes[i].gameObject.name + " : <color=#F00>Not Visible</color>" + "\n";
                    }
                }
            }
            else
            {
                tx += "reflection probe list is null \n";
            }
            tx += "\n";

            //Visible Renderers
            tx += "Renderers : \n";
            if(rens != null)
            {
                for (int i =0;i<rens.Length;i++)
                {
                    if (rens[i].isVisible)
                    {
                        tx += rens[i].gameObject.name + " <color=#0F0>Yes</color> \n";
                    }
                    else
                    {
                        tx += rens[i].gameObject.name + " <color=#F00>No</color> \n";
                    }
                }
                tx += "\n";
            }
            
            //-------------------------------
            //Show debug msg on TextMesh
            //Debug.Log(tx);
            
            if (textMesh != null)
            {
                textMesh.text = tx;
                //Debug.Log("<color=#0F0>TextMesh is updated</color>");
            }
            else
            {
                tx = "<color=#F00>TextMesh is null</color> Please hit play if you hasn't";
                //Debug.Log(tx);
            }
            
            //update = false;
        }
        //============================================== 
    }
}