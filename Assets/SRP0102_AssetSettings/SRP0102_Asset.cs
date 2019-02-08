using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace SRP0102
{
    [ExecuteInEditMode]
    public class SRP0102_Asset : RenderPipelineAsset
    {
        public bool drawOpaqueObjects = true;
        public bool drawTransparentObjects = true;

        protected override RenderPipeline CreatePipeline()
        {
            return new SRP0102(this);
        }

        #if UNITY_EDITOR

        [MenuItem("Assets/Create/Render Pipeline/SRP0102", priority = 1)]
        static void CreateSRP0102()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateSRP0102_Asset>(),"SRP0102.asset", null, null);
        }

        class CreateSRP0102_Asset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<SRP0102_Asset>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        #endif
    }
}
