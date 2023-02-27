using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace SRP0103
{
    [ExecuteInEditMode]
    public class SRP0103_Asset : RenderPipelineAsset<SRP0103>
    {
        public bool drawOpaqueObjects = true;
        public bool drawTransparentObjects = true;

        protected override RenderPipeline CreatePipeline()
        {
            return new SRP0103(this);
        }

        #if UNITY_EDITOR

        [MenuItem("Assets/Create/Render Pipeline/SRP0103", priority = 1)]
        static void CreateSRP0103()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateSRP0103_Asset>(),"SRP0103.asset", null, null);
        }

        class CreateSRP0103_Asset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<SRP0103_Asset>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        #endif
    }
}
