using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System;

#if UNITY_EDITOR
	using UnityEditor;
#endif


[CreateAssetMenu(fileName = "CustomDrawModeAsset", menuName = "CustomDrawModeAsset", order = 1)]
public class CustomDrawModeAsset : ScriptableObject 
{
	#if UNITY_EDITOR

	[Serializable]
	public struct CustomDrawMode
	{
		public string name;
		public string category;
		public Shader shader;
	}
	public CustomDrawMode[] customDrawModes ;

	static CustomDrawModeAsset()
	{
		//CustomDrawModeAssetObject.cdma = this;
	}

	#endif
}

#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
public static class CustomDrawModeAssetObject
{
	public static CustomDrawModeAsset cdma;
	private static Material material;
	private static int previousMode = 99;

	static CustomDrawModeAssetObject()
	{
		SetUpcdma();
	}

	public static void SetUpcdma()
	{
		#if UNITY_EDITOR

		if(cdma == null)
		{
			cdma = (CustomDrawModeAsset)AssetDatabase.LoadAssetAtPath("Assets/SRP0902_SceneViewDrawMode/CustomDrawModeAsset.asset", typeof(CustomDrawModeAsset));
		}
		if(cdma == null)
		{
			Debug.LogError("CustomDrawModeAssetObject is null"); 
		}

		#endif
	}

	public static Material GetDrawModeMaterial()
	{
		#if UNITY_EDITOR

		//Setup object
		SetUpcdma();
		if(cdma == null) return null;

		ArrayList sceneViewsArray = SceneView.sceneViews;
		foreach (SceneView sceneView in sceneViewsArray)
		{
			for (int i=0; i<cdma.customDrawModes.Length; i++)
			{
				if(
					cdma.customDrawModes[i].name != "" && 
					sceneView.cameraMode.name == cdma.customDrawModes[i].name
				)
				{
					if(cdma.customDrawModes[i].shader != null)
					{
						if(i != previousMode)
						{
							if(material == null) material = new Material(cdma.customDrawModes[i].shader);
							else material.shader = cdma.customDrawModes[i].shader;
						}
						previousMode = i;
						return material;
					}
				}
			}
		}

		#endif

		return null;
	}
}
