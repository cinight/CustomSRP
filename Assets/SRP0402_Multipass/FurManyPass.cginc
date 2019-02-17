#ifndef FurManyPass
#define FurManyPass

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 normal: NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NoiseTex;
            float _FurFactor;
            float _FurLayer;

			v2f vert (appdata v)
			{
				v2f o;
				v.vertex += FURLAYER * v.normal * _FurLayer;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) + FURLAYER;
				fixed noise = tex2D(_NoiseTex, i.uv).r;

				clip(noise - FURLAYER * _FurFactor);

				return col;
			}

#endif // FurManyPass