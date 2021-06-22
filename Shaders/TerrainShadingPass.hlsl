#ifndef _TerrainShadingPass_
#define _TerrainShadingPass_

#include "TerrainCommon.hlsl"


struct Attributes
{
    float2 UV0 : TEXCOORD0;
    float4 Vertex_LS : POSITION;
};

struct Varyings
{
    uint InstanceId : SV_InstanceID;
    float2 UV0 : TEXCOORD0;
    float3 Vertex_WS : TEXCOORD2;
    float4 Vertex_CS : SV_POSITION;
};

Varyings ShadingVert(Attributes In, uint InstanceId : SV_InstanceID)
{
    Varyings Out;
    Out.InstanceId = InstanceId + _BufferOffset;
    FSectionData SectionData = _TerrainBuffer[Out.InstanceId];

    float3 WS_Position = MorphVertex(In.UV0, SectionData, In.Vertex_LS.xyz);
    Out.Vertex_CS = mul(UNITY_MATRIX_VP, float4(WS_Position, 1));
    Out.Vertex_WS = WS_Position;
    Out.UV0 = In.UV0;

    return Out;
}

float4 ShadingPixel(const Varyings In) : SV_Target
{
    float3 WorldPosition = In.Vertex_WS.xyz;
    FSectionData SectionData = _TerrainBuffer[In.InstanceId];

    FTerrainCorrd TerrainCorrd;
    float3 SectionPivot = SectionData.SectionPivot.xyz + float3(_SectionSize * 0.5, 0, _SectionSize * 0.5);
    InitTerrainCorrd(TerrainCorrd, _TerrainSize, _SectorSize, _SectionSize, SectionData.NumQuad, SectionData.SectorPivot.xyz, SectionPivot, WorldPosition);

    float3 Normal_WS = _TangentArray.Sample(Global_trilinear_clamp_sampler, float3(TerrainCorrd.SectorUV, SectionData.HeightmapIndex)).rgb * 2 - 1;
    float3 Tangent_WS = cross(unity_ObjectToWorld._13_23_33, Normal_WS);
    float3x3 TangentBasis = float3x3(-Tangent_WS, cross(Normal_WS, Tangent_WS), Normal_WS);

    //Microface
    //float4 Albedo = _DefaultAlbedo.Sample(sampler_DefaultAlbedo, TerrainCorrd.HalfCoordUV, 0);
    //float3 NormalMap = UnpackNormal(_Normal.Sample(Global_trilinear_repeat_sampler, TerrainCorrd.CoordUV, 0)).xyz;
    FSurfaceTexture SurfaceTexture = SampleSurfaceTexture(TerrainCorrd, SectionData);
    float3 TerrainNormal = UnpackNormal(SurfaceTexture.Normal).xyz;
    TerrainNormal = normalize(mul(TerrainNormal, TangentBasis));

    //Shadow
    float4 ShadowCoord = 0;
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        ShadowCoord = TransformWorldToShadowCoord(WorldPosition);
    #endif
    float ShadowTream = MainLightRealtimeShadow(ShadowCoord);

    //Lighting
    float3 DirectDiffuse = saturate(dot(normalize(_MainLightPosition.xyz), TerrainNormal)) * _MainLightColor.rgb * ShadowTream;
    float3 IndirectDiffuse = SampleSH(Normal_WS);
    //float3 IndirectDiffuse = _Cubemap.SampleLevel(Global_bilinear_clamp_sampler, TerrainNormal, 10).rgb;
    
    //return float4(In.UV0, 0, 1);
    //return float4(TerrainCorrd.SectorUV, 0, 1);
    //return float4(SurfaceTexture.Albedo, 1);
    return float4(SurfaceTexture.Albedo * (DirectDiffuse + IndirectDiffuse), 1);
    //return float4((TerrainCorrd.SectorUV * (_TerrainSize - 1.0f) + 0.5f) * rcp(_TerrainSize), 0, 1);
}

#endif