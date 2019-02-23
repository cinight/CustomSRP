Shader "CustomSRP/SRP0803/MRT UnlitTransparent"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Back Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Tags { "LightMode" = "SRP0803_Pass" }

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

				o.Emission = 1-o.Emission; //just want a different color for transparent objects..

				return o;
			}
			ENDCG
		}
	}
}
