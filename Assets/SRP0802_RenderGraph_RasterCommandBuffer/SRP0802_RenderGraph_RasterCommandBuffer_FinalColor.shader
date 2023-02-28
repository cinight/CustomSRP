Shader "Hidden/CustomSRP/SRP0802_RasterCommandBuffer/FinalColor"
{
    Properties
    {
    }

    SubShader
    {
        Pass
        {
            Name "FinalColor"

            // No culling or depth
            ZWrite Off ZTest Always Blend Off Cull Off
	
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"
			#include "Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			//Reference to com.unity.render-pipelines.universal\Shaders\Utils\CoreBlit.shader to use with the Blitter class

			sampler2D _CameraAlbedoTexture;
			sampler2D _CameraEmissionTexture;

			float4 frag (Varyings input) : SV_Target
			{
				float2 uv = input.texcoord;
				#if UNITY_UV_STARTS_AT_TOP
				uv.y = -uv.y;
				#endif

				float4 albedo = tex2D(_CameraAlbedoTexture, uv);
				float4 emission = tex2D(_CameraEmissionTexture, uv);

				return albedo + emission;
			}
			ENDHLSL
        }
    }
}