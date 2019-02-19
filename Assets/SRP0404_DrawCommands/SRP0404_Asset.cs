using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace SRP0404
{
    [ExecuteInEditMode]
    public class SRP0404_Asset : RenderPipelineAsset
    {
        public Mesh mesh;
        public Material mat;
        public int count = 1;

        protected override RenderPipeline CreatePipeline()
        {
            return new SRP0404(this);
        }

        #if UNITY_EDITOR

        [MenuItem("Assets/Create/Render Pipeline/SRP0404", priority = 1)]
        static void CreateSRP0404()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateSRP0404_Asset>(),"SRP0404.asset", null, null);
        }

        class CreateSRP0404_Asset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<SRP0404_Asset>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        #endif
    }
}
