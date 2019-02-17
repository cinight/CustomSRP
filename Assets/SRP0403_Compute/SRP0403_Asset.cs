using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace SRP0403
{
    [ExecuteInEditMode]
    public class SRP0403_Asset : RenderPipelineAsset
    {
        public ComputeShader computeShader;
        public float edgeDetect = 1;
        public int detectRange = 1;

        protected override RenderPipeline CreatePipeline()
        {
            return new SRP0403(this);
        }

        #if UNITY_EDITOR

        [MenuItem("Assets/Create/Render Pipeline/SRP0403", priority = 1)]
        static void CreateSRP0403()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateSRP0403_Asset>(),"SRP0403.asset", null, null);
        }

        class CreateSRP0403_Asset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<SRP0403_Asset>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        #endif
    }
}
