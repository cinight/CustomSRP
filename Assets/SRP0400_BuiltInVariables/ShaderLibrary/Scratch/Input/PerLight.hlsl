#ifndef SCRATCH_INPUT_PERLIGHT_HLSL
#define SCRATCH_INPUT_PERLIGHT_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(_PerLight)
float4 _LightData;
float3 _LightColor;
CBUFFER_END

#endif