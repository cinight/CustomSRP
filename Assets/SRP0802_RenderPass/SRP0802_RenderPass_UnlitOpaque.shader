Shader "CustomSRP/SRP0802/RenderPass UnlitOpaque"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			Tags { "LightMode" = "SRP0802_Pass1" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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

			struct RTstruct
			{
				float4 Albedo : SV_Target0;
				float4 Emission : SV_Target1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			RTstruct frag (v2f i)
			{
				RTstruct o;

				o.Albedo = tex2D(_MainTex, i.uv);
				o.Emission = frac(float4(_Time.x, _Time.y, _Time.z, _Time.w));
				o.Emission.xy *= i.uv;
				o.Emission.zw *= i.uv;
				
				return o;
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "SRP0802_Pass2" }
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "HLSLSupport.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(0);
			UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(1);

			float4 frag (v2f i) : SV_Target0
			{
				float4 albedo = UNITY_READ_FRAMEBUFFER_INPUT(0, i.vertex.xyz);
				float4 emission = UNITY_READ_FRAMEBUFFER_INPUT(1, i.vertex.xyz);

				return albedo + emission;
			}
			ENDCG
		}
	}
}
