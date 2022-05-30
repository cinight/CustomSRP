//Remember to add ENABLE_SHADER_DEBUG_PRINT in ProjectSettings > Player
Shader "CustomSRP/SRP0406/UnlitOpaque"
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
			Tags { "LightMode" = "SRP0406_Pass" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ShaderDebugPrint.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
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
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv) * _Color;

				//Shader debug print, we only take the current cursor position pixel
				float4 screenPos = i.screenPos / i.screenPos.w;
				screenPos.xy = floor(screenPos.xy * _ScreenParams.xy);
				float debugDist = distance(screenPos.xy,_ShaderDebugPrintInputMouse.xy);
				debugDist = round(debugDist);
				if(debugDist < 1)
				{
					ShaderDebugPrintMouseButtonOver(_ShaderDebugPrintInputMouse.xy,col);
				}

				return col;
			}
			ENDHLSL
		}
	}
}
