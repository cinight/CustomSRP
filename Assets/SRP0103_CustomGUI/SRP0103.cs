using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP0103
{
    public class SRP0103 : RenderPipeline
    {
        private static SRP0103_Asset m_PipelineAsset;
        private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0103_Pass"); //The shader pass tag just for SRP0103

        public SRP0103(SRP0103_Asset pipelineAsset)
        {
            m_PipelineAsset = pipelineAsset;
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
                if(drawSkyBox)  {  context.DrawSkybox(camera);  }

                //Opaque objects
                if(m_PipelineAsset.drawOpaqueObjects) //Use the settings on the asset
                {
                    sortingSettings.criteria = SortingCriteria.CommonOpaque;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.opaque;
                    context.DrawRenderers(cull, ref drawSettings, ref filterSettings);
                }

                //Transparent objects
                if(m_PipelineAsset.drawTransparentObjects) //Use the settings on the asset
                {
                    sortingSettings.criteria = SortingCriteria.CommonTransparent;
                    drawSettings.sortingSettings = sortingSettings;
                    filterSettings.renderQueueRange = RenderQueueRange.transparent;
                    context.DrawRenderers(cull, ref drawSettings, ref filterSettings);
                }

                context.Submit();
                
                EndCameraRendering(context,camera);
            }

            EndFrameRendering(context,cameras);
        }
    }
}