    Shader "DrawInstancedIndirect" 
    {
    Properties 
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader 
    {
        Pass 
        {
            Tags { "LightMode" = "SRP0404_Pass" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "../_General/ShaderLibrary/Input/Transformation.hlsl"

            sampler2D _MainTex;
            #if SHADER_TARGET >= 45
                StructuredBuffer<float4> positionBuffer;
            #endif

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD;
            };

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                #if SHADER_TARGET >= 45
                    float4 data = positionBuffer[instanceID];
                #else
                    float4 data = 0;
                #endif

                float3 localPosition = v.vertex.xyz * data.w;
                float3 worldPosition = data.xyz + localPosition;

                v2f o;
                o.pos = TransformWorldToHClip(worldPosition);//mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv = v.texcoord;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDHLSL
        }
    }
}