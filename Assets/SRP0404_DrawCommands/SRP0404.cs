using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SRP0404
{
    public class SRP0404 : RenderPipeline
    {
        private static SRP0404_Asset m_PipelineAsset;
        private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0404_Pass"); //The shader pass tag just for SRP0404

        private List<Vector4> positions;
        private ComputeBuffer positionBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private MaterialPropertyBlock m_MaterialPropertyBlock = new MaterialPropertyBlock();

        public SRP0404(SRP0404_Asset pipelineAsset)
        {
            m_PipelineAsset = pipelineAsset;
            positions = new List<Vector4>();
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

                //Define positions
                positions.Clear();
                Vector4 pos = Vector4.zero;
                int splitRow = 5;
                for(int i=0; i<m_PipelineAsset.count; i++)
                {
                    pos.x = i % splitRow;
                    pos.y = Mathf.FloorToInt(i / splitRow);
                    pos.z = 0;
                    pos.w = 1;
                    positions.Add(pos);
                }

                //SetUp Position Computebuffers Data
                positionBuffer = new ComputeBuffer(m_PipelineAsset.count, 16); //4*4 bytes for Vector4
                positionBuffer.SetData(positions);
                m_MaterialPropertyBlock.SetBuffer("positionBuffer", positionBuffer);

                //SetUp Args Computebuffers Data
                argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                args[0] = (uint)m_PipelineAsset.mesh.GetIndexCount(0);
                args[1] = (uint)m_PipelineAsset.count;
                args[2] = (uint)m_PipelineAsset.mesh.GetIndexStart(0);
                args[3] = (uint)m_PipelineAsset.mesh.GetBaseVertex(0);
                argsBuffer.SetData(args);

                //Draw Commands
                CommandBuffer cmdDraw = new CommandBuffer();
                cmdDraw.DrawMeshInstancedIndirect(m_PipelineAsset.mesh,0,m_PipelineAsset.mat,0,argsBuffer,0,m_MaterialPropertyBlock);
                context.ExecuteCommandBuffer(cmdDraw);
                cmdDraw.Release();

                context.Submit();

                //CleanUp after rendering this cam
                if (positionBuffer != null)
                {
                    positionBuffer.Release();
                    positionBuffer = null;
                }
                if (argsBuffer != null)
                {
                    argsBuffer.Release();
                    argsBuffer = null;
                }
            }
        }
    }
}