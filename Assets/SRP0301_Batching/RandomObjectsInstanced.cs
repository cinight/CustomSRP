using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomObjectsInstanced : MonoBehaviour 
{
	public Renderer[] renderers;

	void Start () 
	{
		Randomize();
	}
	
	[ContextMenu ("Update List")]
	public void UpdateList()
	{
		renderers = GetComponentsInChildren<Renderer>();
	}

	public void Randomize()
	{
		if(renderers==null) UpdateList();
		if(renderers==null) return;

		MaterialPropertyBlock props = new MaterialPropertyBlock();
		
		for(int i = 0; i <renderers.Length; i++)
		{
			//Random color on material
			for(int j = 1; j <=16 ; j++)
			{
				float f = (float)j/16.0f;
				float k = (float)i/renderers.Length;
				Color col = Color.HSVToRGB( f*k , 1.0f , 1.0f ) * 0.1f;

				props.SetColor("_Color"+j, col );
			}
			renderers[i].SetPropertyBlock(props);
		}
	}
}
