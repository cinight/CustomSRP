Shader "CustomSRP/SRP0703/UnlitOpaque"
{
	Properties
	{
		[Header(Main)]
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		
		[Header(Emission)]
		[HDR]_EmissionColor("Emission Color", Color) = (1,1,1,1)
		[NoScaleOffset] _EmissionTex ("Emission (RGBA)", 2D) = "Black" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			Tags { "LightMode" = "SRP0703_Pass" }

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			sampler2D _EmissionTex;
			float4 _EmissionColor;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv) * _Color;
				float4 emission = tex2D(_EmissionTex, i.uv) * _EmissionColor;
				return col + emission;
			}
			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "SRP0703_MotionVector" }

			ZWrite Off
			//ColorMask 0 1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4x4 _NonJitteredViewProjMatrix;
			float4x4 _PrevViewProjMatrix;
			float4x4 _PreviousM;

			struct appdata
			{
				float4 position : POSITION;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 transfer0 : TEXCOORD0;
				float4 transfer1 : TEXCOORD1;
			};

			// struct Transform
			// {
			// 	float4x4 instanceToObject;
			// 	float4x4 objectToInstance;
			// };

			v2f vert(appdata input)
			{
				//Transform anim0 = CalculateAnimation(input.instanceID, _CurrentTime - _DeltaTime);
				//Transform anim1 = CalculateAnimation(input.instanceID, _CurrentTime);

				//float4 vp0 = mul(anim0.instanceToObject, input.position);
				//float4 vp1 = mul(anim1.instanceToObject, input.position);

				float4 vp0 = mul(unity_ObjectToWorld, input.position);

				v2f o;
				o.position = UnityObjectToClipPos(input.position);
				o.transfer0 = mul(_PrevViewProjMatrix, mul(_PreviousM, vp0));
				o.transfer1 = mul(_NonJitteredViewProjMatrix, mul(unity_ObjectToWorld, vp0));
				return o;
			}

			float4 frag(v2f input) : SV_Target
			{
				float3 hp0 = input.transfer0.xyz / input.transfer0.w;
				float3 hp1 = input.transfer1.xyz / input.transfer1.w;

				float2 vp0 = (hp0.xy + 1) / 2;
				float2 vp1 = (hp1.xy + 1) / 2;

				#if UNITY_UV_STARTS_AT_TOP
					vp0.y = 1 - vp0.y;
					vp1.y = 1 - vp1.y;
				#endif

				return half4(vp1 - vp0, 0, 1);
			}

			ENDCG
		}
	}
}
