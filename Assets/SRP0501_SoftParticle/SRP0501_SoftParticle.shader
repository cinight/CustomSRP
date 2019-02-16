Shader "CustomSRP/SRP0501/SoftParticle"
{
    Properties 
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0

        _EdgeAroundColor("Edge Color", Color) = (1,1,1,1)
        _EdgeAroundPower("Edge Color Power",Range(1,10)) = 1
    }

    SubShader 
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back Lighting Off ZWrite On //Set to on, so that the later transparent objects will render correctly

        Pass 
        {
            Tags { "LightMode" = "SRP0501_Pass" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ _FLIPUV

            #include "UnityCG.cginc"


            struct appdata_t 
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f 
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 projPos : TEXCOORD2;
            };

            sampler2D _CameraDepthTexture;
            float _InvFade;
            sampler2D _MainTex;
            float4 _MainTex_ST;
       
            fixed4 _TintColor;
			fixed4 _EdgeAroundColor;
			fixed _EdgeAroundPower;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos (o.vertex);

                COMPUTE_EYEDEPTH(o.projPos.z);
                o.texcoord = v.texcoord;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half4 col = tex2D(_MainTex, i.texcoord);

                    float2 uv = i.projPos.xy/ i.projPos.z;

                    //#if defined(UNITY_UV_STARTS_AT_TOP) && !defined(_FLIPUV)
                    //    uv.y = 1- uv.y;
                    //#endif

                    float sceneZ = LinearEyeDepth (tex2D(_CameraDepthTexture, uv));
                    float partZ = i.projPos.z;
                    float fZ = (sceneZ-partZ);
                    float fade = saturate (_InvFade * fZ);
                    col.a *= fade;

                    float edgearound = pow( fade *_EdgeAroundColor.a, _EdgeAroundPower);
                    col.rgb = lerp( _EdgeAroundColor.rgb, col.rgb, edgearound);

                    //float depth = tex2D(_CameraDepthTexture, i.texcoord).r * 10;
                    //float4 c = float4(depth, 0 ,0,1);

                return col*0.8f;
            }
            ENDCG
        }
    }
}