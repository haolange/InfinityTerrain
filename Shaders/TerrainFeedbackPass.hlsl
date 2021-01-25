#ifndef _TerrainFeedbackPass_
#define _TerrainFeedbackPass_

#include "TerrainCommon.hlsl"

float4 _VTFeedbackParams; //xy : PagePivot, z : PageWorlsSize, w : Physcis TexSize

struct Attributes
{
    float2 UV0 : TEXCOORD0;
    float4 Vertex_LS : POSITION;
};

struct Varyings
{
    uint InstanceId : SV_InstanceID;
    float2 PageUV : TEXCOORD1;
    float4 Vertex_CS : SV_POSITION;
};

Varyings FeedbackVert(Attributes In, uint InstanceId : SV_InstanceID)
{
    Varyings Out;
    Out.InstanceId = InstanceId + _BufferOffset;
    FSectionData SectionData = _TerrainBuffer[Out.InstanceId];

    //float3 WS_Position = In.Vertex_LS.xyz + SectionData.SectionPivot.xyz;
    float3 WS_Position = MorphVertex(In.UV0, SectionData, In.Vertex_LS.xyz);
    Out.PageUV = (WS_Position.xz - _VTFeedbackParams.xy) * rcp(_VTFeedbackParams.z);
    Out.Vertex_CS = mul(UNITY_MATRIX_VP, float4(WS_Position, 1));

    return Out;
}

float4 FeedbackPixel(const Varyings In) : SV_Target
{
	float ComputedLevel = min(6, MipLevel(In.PageUV * _VTFeedbackParams.w)) /* 0.5 - 0.25*/;
    //return float4(In.PageUV, 1, 1);
    //return floor(ComputedLevel) / 255.0f;
	return float4(In.PageUV, floor(ComputedLevel) / 255.0f, 1);
}

#endif