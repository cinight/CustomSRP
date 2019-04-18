Shader "CustomSRP/SRP0603/LitOpaque"
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
			Tags { "LightMode" = "SRP0603_Pass" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "SRP0603_RealtimeLights.hlsl"
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
    			float3 normalOS : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normalWS : NORMAL;
				float3 positionWS : TEXCOORD1;
				float4 vertex : SV_POSITION;

				float4 _ShadowCoord : TEXCOORD2;
			};

			// Declaring our per-material properties in a constant buffer will allow the new SRP batcher to function.
			// It's a good habit to get into early, as it's an easy performance win that doesn't obscure your code.
			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			CBUFFER_END

			#define MAXLIGHTCOUNT 16

			CBUFFER_START(_LightBuffer)
				float4 _LightColorArray[MAXLIGHTCOUNT];
				float4 _LightDataArray[MAXLIGHTCOUNT];
				float4 _LightSpotDirArray[MAXLIGHTCOUNT];
			CBUFFER_END

			sampler2D _ShadowMapTexture;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float4 positionCS = TransformWorldToHClip(positionWS);
		
				o.vertex = positionCS;
				o.positionWS = positionWS;
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o._ShadowCoord = ComputeScreenPos(o.vertex);
				return o;
			}
			
			float4 CalculateLight(v2f IN, int i, float4 albedo)
			{
				if (_LightColorArray[i].w == -1) //-1 is directional
				{
					albedo.rgb += ShadeDirectionalLight(IN.normalWS, albedo.rgb, _LightDataArray[i].xyz, _LightColorArray[i].rgb);
				}
				else if (_LightColorArray[i].w == -2) //-2 is pointlight
				{
					albedo.rgb += ShadePointLight(IN.normalWS, albedo.rgb, IN.positionWS, _LightDataArray[i].xyz, _LightDataArray[i].w, _LightColorArray[i].rgb);
				}
				else //Spotlight
				{
					albedo.rgb += ShadeSpotLight(IN.normalWS, albedo.rgb, IN.positionWS, _LightDataArray[i], _LightSpotDirArray[i], _LightColorArray[i]);
				}

				return albedo;
			}

			float4 frag (v2f IN) : SV_Target
			{
				float4 albedo = tex2D(_MainTex, IN.uv) * _Color;

				for (int id = 0; id < min(unity_LightData.y,4); id++) 
				{
					int i = unity_LightIndices[0][id];
					albedo = CalculateLight(IN, i,albedo);
				}

				for (int id2 = 4; id2 < min(unity_LightData.y,8); id2++) 
				{
					int i = unity_LightIndices[1][id2-4];
					albedo = CalculateLight(IN, i,albedo);
				}

				//Shadow
				float attenuation = tex2Dproj(_ShadowMapTexture, IN._ShadowCoord).r;
				albedo.rgb *= attenuation;
				
				return albedo;
			}
			ENDHLSL
		}
		//=======================================================================
        Pass
     	{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }

         	HLSLPROGRAM
 			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile_shadowcaster
			//#pragma fragmentoption ARB_precision_hint_fastest
 
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"
 
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};
 
			v2f vert(appdata v)
			{
				v2f o;

				float4 wPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1));
				//float3 wNormal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));

					//float3 wLight = normalize(_WorldSpaceLightPos0.xyz);

					//float shadowCos = dot(wNormal, wLight);
					//float shadowSine = sqrt(1-shadowCos*shadowCos);
					//float normalBias = unity_LightShadowBias.z * shadowSine;
					//float normalBias = 0.01f * shadowSine;

					//wPos.xyz -= wNormal * normalBias;

				o.pos = mul(UNITY_MATRIX_VP, wPos);
				//o.pos = UnityApplyLinearShadowBias(o.pos);

				#if UNITY_REVERSED_Z
					o.pos.z = min(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					o.pos.z = max(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
				#endif

				return o;
			}
 
			float4 frag(v2f i) : SV_Target
			{
				return 0;
			}
 
         	ENDHLSL
    	}
	}
}