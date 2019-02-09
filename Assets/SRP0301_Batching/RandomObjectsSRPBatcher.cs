using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomObjectsSRPBatcher : MonoBehaviour 
{
	//public Texture[] texs;
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

	//For assigning properties
	//int texid = 0;

	public void Randomize()
	{
		if(renderers==null) UpdateList();
		if(renderers==null) return;

		for(int i = 0; i <renderers.Length; i++)
		{
			//Make instance material
			Material mat = renderers[i].material;
			mat.name = "Mat object "+i;

			//Random texture on material
			// if(texid >= texs.Length) texid = 0;
			// mat.SetTexture("_MainTex",texs[texid]);
			// texid++;

			//Random color on material
			for(int j = 1; j <=16 ; j++)
			{
				float f = (float)j/16.0f;
				float k = (float)i/renderers.Length;
				Color col = Color.HSVToRGB( f*k , 1.0f , 1.0f ) * 0.1f;

				mat.SetColor("_Color"+j, col );
			}
		}

	}


}
