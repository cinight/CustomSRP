Shader "CustomSRP/SRP0902/Debug/Depth"
{
    SubShader 
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }

        Pass 
        {
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t 
            {
                float4 vertex : POSITION;
            };

            struct v2f 
            {
                float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD0;
            };

            sampler2D _CameraDepthTexture;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos (o.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.projPos.xy/ i.projPos.z;
                float sceneZ = LinearEyeDepth (tex2D(_CameraDepthTexture, uv));
                float partZ = i.projPos.z;
                float fZ = (sceneZ-partZ);

                float4 col;
                col.rgb = abs(sceneZ) * 0.1f;
                col.a=1;

                return col;
            }
            ENDCG
        }
    }
}