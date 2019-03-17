Shader "CustomSRP/SRP0602/ReflectionProbe"
{
	Properties
	{
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

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 reflectionDir : TEXCOORD0;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);

				float3 posWS = TransformObjectToWorld(v.vertex.xyz);
				float3 viewDir = posWS - _WorldSpaceCameraPos;
				o.reflectionDir = reflect(viewDir, v.normal);

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				//float4 col = unity_SpecCube0.SampleLevel(samplerunity_SpecCube0, i.reflectionDir,0);
				float4 col = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0, i.reflectionDir,0);
				return col;
			}
			ENDHLSL
		}
	}
}
