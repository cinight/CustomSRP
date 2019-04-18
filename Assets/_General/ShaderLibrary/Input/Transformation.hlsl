#ifndef SCRATCH_INPUT_TRANSFORMATION_HLSL
#define SCRATCH_INPUT_TRANSFORMATION_HLSL

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  ERROR_UNITY_MATRIX_I_VP_IS_NOT_DEFINED
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)

#include "UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// Below functions are missing in core RP...

float3 TransformObjectToViewPos(float3 positionOS)
{
    return mul(GetWorldToViewMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0))).xyz;
}

float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

float4 ComputeGrabScreenPos (float4 pos) 
{
    #if UNITY_UV_STARTS_AT_TOP
        float scale = -1.0;
    #else
        float scale = 1.0;
    #endif

    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y*scale) + o.w;

    #ifdef UNITY_SINGLE_PASS_STEREO
        o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
    #endif
    
    o.zw = pos.zw;
    return o;
}

#endif // SCRATCH_INPUT_TRANSFORMATION_HLSL