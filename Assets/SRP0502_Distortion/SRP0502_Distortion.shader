Shader "CustomSRP/SRP0502/Distortion"
{
	Properties
	{
		_Color ("Color",Color) = (1,1,1,1)
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Noise("Noise", Range(0, 0.3)) = 0.3
	}

	SubShader
	{
		//Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		//Blend SrcAlpha OneMinusSrcAlpha

		Cull Off Lighting Off ZWrite On

		Pass
		{
			Tags { "LightMode" = "SRP0502_Distortion" }

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
				float4 grabPos : TEXCOORD1;
			};

			sampler2D _CameraColorTexture;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Noise;
			float4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.grabPos = ComputeGrabScreenPos(o.vertex);

				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 bguv = i.grabPos;
				float2 nuv = bguv.xy / bguv.w;
				
				// #ifdef UNITY_UV_STARTS_AT_TOP
				// 	nuv.y = 1-nuv.y;
				// #endif

				float mask = tex2D(_MainTex,i.uv).a;
				mask = saturate(mask) * _Color.a;
				
				//Distoriton UV
				float noise = sin(nuv*30.0f);
				float2 duv = lerp(nuv-noise*_Noise,nuv+noise*_Noise,_SinTime.w);

				float4 bg = tex2D(_CameraColorTexture, lerp(nuv,duv,mask)); //The background texture
				bg.rgb = lerp( bg.rgb , bg.rgb*_Color.rgb , mask );
				

				////return col;
				return bg;
			}
			ENDCG
		}
	}
}