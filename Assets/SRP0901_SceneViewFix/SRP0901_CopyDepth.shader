Shader "Hidden/CustomSRP/SRP0901/CopyDepth"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "CopyDepth"
            ZTest Always ZWrite On ColorMask 0

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _CameraDepthTexture;

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;
                o.uv = i.uv;
                o.position = UnityObjectToClipPos(i.vertex.xyz);
                return o;
            }

            float frag(VertexOutput i) : SV_Depth
            {
                float2 uv = i.uv;
                return tex2D(_CameraDepthTexture, uv);
            }

            ENDCG
        }
    }
}