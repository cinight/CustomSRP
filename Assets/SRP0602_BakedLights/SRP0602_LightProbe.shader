Shader "CustomSRP/SRP0602/LightProbe"
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
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normalWS : NORMAL;
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
				o.normalWS = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
				return o;
			}

			// Samples SH L0, L1 and L2 terms
			half3 SampleSH(half3 normalWS)
			{
				// LPPV is not supported in Ligthweight Pipeline
				real4 SHCoefficients[7];
				SHCoefficients[0] = unity_SHAr;
				SHCoefficients[1] = unity_SHAg;
				SHCoefficients[2] = unity_SHAb;
				SHCoefficients[3] = unity_SHBr;
				SHCoefficients[4] = unity_SHBg;
				SHCoefficients[5] = unity_SHBb;
				SHCoefficients[6] = unity_SHC;

				return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
			}

			float4 frag (v2f i) : SV_Target
			{
				float3 currentAmbient = float3(0, 0, 0);
				float3 lightProbe = SampleSH(i.normalWS);//SampleSHPixel(i.vertexSH, i.normalWS);

				float4 col = tex2D(_MainTex, i.uv) * _Color;
				return col + float4(lightProbe,1);
			}
			ENDHLSL
		}
	}
}
