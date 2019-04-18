Shader "Hidden/My/ScreenSpaceShadows"
{
    SubShader
    {
        //Tags{ "RenderPipeline" = "ForwardBase" "IgnoreProjector" = "True"}

        Pass
        {
            Name "ScreenSpaceShadows"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vertex
            #pragma fragment Fragment

            #include "../_General/ShaderLibrary/Input/Transformation.hlsl"

            Texture2D _CameraDepthTexture;
            SamplerState sampler_CameraDepthTexture;

            Texture2D _ShadowMap;
            SamplerComparisonState sampler_ShadowMap;
            SamplerState sampler_ShadowMap_state;
            float _ShadowBias;

            float4x4 _WorldToShadow;
            float _ShadowStrength;

            //I want to blend together
            sampler2D _ShadowMapTexture;

            struct VertexInput
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;

                float2 uv : TEXCOORD1;
            };

            struct v2f
            {
                half4  pos      : SV_POSITION;
                half4  texcoord : TEXCOORD0;

                float2 uv : TEXCOORD1;
            };

            v2f Vertex(VertexInput i)
            {
                v2f o;

                o.pos = TransformObjectToHClip(i.vertex.xyz);

                float4 projPos = o.pos * 0.5;
                projPos.xy = projPos.xy + projPos.w;

                o.texcoord.xy = i.texcoord;
                o.texcoord.zw = projPos.xy;

                o.uv = i.uv;

                return o;
            }

            half4 Fragment(v2f i) : SV_Target
            {
                float deviceDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, i.texcoord.xy);

                #if UNITY_REVERSED_Z
                    deviceDepth = 1 - deviceDepth;
                #endif
                    deviceDepth = 2 * deviceDepth - 1; //NOTE: Currently must massage depth before computing CS position.

                //Clip
                float4 positionCS = float4(i.texcoord.zw * 2.0 - 1.0, deviceDepth, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    positionCS.y = -positionCS.y;
                #endif
                    // _ShadowBias.x sign depens on if platform has reversed z buffer
                positionCS.z += _ShadowBias;

                //View
                float4 positionVS = mul(unity_CameraInvProjection, positionCS);
                positionVS.z = -positionVS.z; // The view space uses a right-handed coordinate system.
                float3 vpos =  positionVS.xyz / positionVS.w;

                //World
                float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;

                //Fetch shadow coordinates for cascade.
                //float4 coords  = mul(wts, float4(wpos, 1.0));
                float4 coords  = mul(_WorldToShadow, float4(wpos, 1.0));

                coords.xyz /= coords.w;
                float attenuation = _ShadowMap.SampleCmpLevelZero(sampler_ShadowMap, coords.xy, coords.z);
                float oneMinusT = 1.0 - _ShadowStrength;
                attenuation = oneMinusT + attenuation * _ShadowStrength;

                float4 final = tex2D(_ShadowMapTexture,i.uv);
                final.rgb *= attenuation;

                return final;
            }

            ENDHLSL
        }
    }
}