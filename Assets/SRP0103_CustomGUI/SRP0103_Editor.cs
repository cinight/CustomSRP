using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SRP0103
{
    [CustomEditor(typeof(SRP0103_Asset))]
    public class SRP0103_Editor : Editor
    {
        bool togglegroup1 = false;

        //The properties from the pipeline assets
        SerializedProperty m_drawOpaqueObjects;
        SerializedProperty m_drawTransparentObjects;

        void OnEnable()
        {
            m_drawOpaqueObjects = serializedObject.FindProperty("drawOpaqueObjects");
            m_drawTransparentObjects = serializedObject.FindProperty("drawTransparentObjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.color = Color.cyan;
            GUILayout.Label ("SRP0103", EditorStyles.boldLabel);
            GUILayout.Label ("This pipeline shows how to create custom inspector for the RenderPipelineAsset", EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
            GUILayout.Space(15);

            togglegroup1 = EditorGUILayout.BeginToggleGroup ("Show Settings", togglegroup1);
                EditorGUI.indentLevel++;
                m_drawOpaqueObjects.boolValue = EditorGUILayout.Toggle("Draw Opaque Objects",m_drawOpaqueObjects.boolValue);
                m_drawTransparentObjects.boolValue = EditorGUILayout.Toggle("Draw Transparent Objects",m_drawTransparentObjects.boolValue);
                EditorGUI.indentLevel--;
            EditorGUILayout.EndToggleGroup();

            GUILayout.Space(15);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

