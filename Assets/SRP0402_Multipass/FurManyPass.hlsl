#ifndef FurManyPass
#define FurManyPass

			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"

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

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NoiseTex;
            float _FurFactor;
            float _FurLayer;
			CBUFFER_END

			v2f vert (appdata v)
			{
				v2f o;
				v.vertex += FURLAYER * v.normal * _FurLayer;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv) + FURLAYER;
				float noise = tex2D(_NoiseTex, i.uv).r;

				clip(noise - FURLAYER * _FurFactor);

				return col;
			}

#endif // FurManyPass