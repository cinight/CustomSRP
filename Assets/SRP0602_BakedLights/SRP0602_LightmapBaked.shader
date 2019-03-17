Shader "CustomSRP/SRP0602/LightmapBaked"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			Tags { "LightMode" = "SRP0602_Pass" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 lightmapUV : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 lightmapUV : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			CBUFFER_END
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.lightmapUV = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 lightmap = SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, i.lightmapUV);
				float4 col = tex2D(_MainTex, i.uv) * _Color;

				return col * lightmap;
			}
			ENDHLSL
		}
	}
}
