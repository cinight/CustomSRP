Shader "CustomSRP/SRP0301/UnlitOpaque SRPBatcher"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color1 ("Color 1", Color) = (1,1,1,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Color3 ("Color 3", Color) = (1,1,1,1)
		_Color4 ("Color 4", Color) = (1,1,1,1)
		_Color5 ("Color 5", Color) = (1,1,1,1)
		_Color6 ("Color 6", Color) = (1,1,1,1)
		_Color7 ("Color 7", Color) = (1,1,1,1)
		_Color8 ("Color 8", Color) = (1,1,1,1)
		_Color9 ("Color 9", Color) = (1,1,1,1)
		_Color10 ("Color 10", Color) = (1,1,1,1)
		_Color11 ("Color 11", Color) = (1,1,1,1)
		_Color12 ("Color 12", Color) = (1,1,1,1)
		_Color13 ("Color 13", Color) = (1,1,1,1)
		_Color14 ("Color 14", Color) = (1,1,1,1)
		_Color15 ("Color 15", Color) = (1,1,1,1)
		_Color16 ("Color 16", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			Tags { "LightMode" = "SRP0301_Pass" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;

			CBUFFER_START(UnityPerDraw)
			float4x4 unity_ObjectToWorld;
			float4x4 unity_WorldToObject;
			float4 unity_LODFade;
			float4 unity_WorldTransformParams;
			CBUFFER_END

			CBUFFER_START(UnityPerFrame)
			float4x4 unity_MatrixVP;
			CBUFFER_END

			UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityPerMaterial) //SRPBatcher	(respect block)
			float4 _MainTex_ST;
			float4 _Color1;
			float4 _Color2;
			float4 _Color3;
			float4 _Color4;
			float4 _Color5;
			float4 _Color6;
			float4 _Color7;
			float4 _Color8;
			float4 _Color9;
			float4 _Color10;
			float4 _Color11;
			float4 _Color12;
			float4 _Color13;
			float4 _Color14;
			float4 _Color15;
			float4 _Color16;
			UNITY_INSTANCING_CBUFFER_SCOPE_END //SRPBatcher
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D( _MainTex, i.uv );

				float4 color = _Color1;
				color += _Color2;
				color += _Color3;
				color += _Color4;
				color += _Color5;
				color += _Color6;
				color += _Color7;
				color += _Color8;
				color += _Color9;
				color += _Color10;
				color += _Color11;
				color += _Color12;
				color += _Color13;
				color += _Color14;
				color += _Color15;
				color += _Color16;
				
				return col * color;
			}
			ENDHLSL
		}
	}
}
