Shader "CustomSRP/SRP0301/UnlitOpaque GPUInstancing"
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

		Pass
		{
			Tags { "LightMode" = "SRP0301_Pass" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;

			//D3D 64KB * 500 Objects OPENGL 16KB * 125 Objects
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color1)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color2)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color3)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color4)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color5)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color6)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color7)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color8)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color9)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color10)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color11)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color12)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color13)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color14)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color15)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color16) //16 bytes * 16 colors = 256 bytes
			UNITY_INSTANCING_BUFFER_END(Props)
			
			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				fixed4 col = tex2D( _MainTex, i.uv );

				fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color1);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color2);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color3);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color4);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color5);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color6);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color7);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color8);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color9);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color10);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color11);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color12);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color13);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color14);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color15);
				color += UNITY_ACCESS_INSTANCED_PROP(Props, _Color16);
				
				return col * color;
			}
			ENDCG
		}
	}
}
