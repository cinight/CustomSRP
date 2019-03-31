Shader "MotionVectorsDebug"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MinMax ("MinMax", Vector) = (0,0,0,0)
		_MotionMultiplier ("Motion Vector Multiplier", float) = 1
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragMotionVectors
			#include "UnityCG.cginc"

			sampler2D _CameraMotionVectorsTexture;
			float4 _MinMax;
			float _MotionMultiplier;

			// Convert a motion vector into RGBA color.
			float4 VectorToColor(float2 mv)
			{
				float phi = atan2(mv.x, mv.y);
				float hue = (phi / UNITY_PI + 1.0) * 0.5;

				float r = abs(hue * 6.0 - 3.0) - 1.0;
				float g = 2.0 - abs(hue * 6.0 - 2.0);
				float b = 2.0 - abs(hue * 6.0 - 4.0);
				float a = length(mv);

				return saturate(float4(r, g, b, a));
			}

			fixed4 fragMotionVectors (v2f_img i) : SV_Target
			{
				float4 movecs = tex2D(_CameraMotionVectorsTexture, i.uv) * _MotionMultiplier;
				float4 rgbMovecs = VectorToColor(movecs.rg);
				return fixed4(rgbMovecs.rgb, 1);
			}

            ENDCG
        }
	}
}