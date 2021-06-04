#ifndef _TerrainCommon_
#define _TerrainCommon_

//#include "Assets/Scripts/CustomURP/HGURP/ShaderLibrary/Lighting.hlsl"
//#include "Assets/Scripts/CustomURP/HGURP/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct FSectionData
{
    int NumQuad;
    int LODIndex;
    int HeightmapIndex;
    int SplatmapIndex;
    int SplatmapCount;
    int SurfacemapCount;
    int4 SurfacemapAIndex;
    int4 SurfacemapBIndex;
    //int4 SurfacemapCIndex;
    //int4 SurfacemapDIndex;
    float ScaleY;
    float FractionLOD;
    float Top_FractionLOD;
    float Buttom_FractionLOD;
    float Left_FractionLOD;
    float Right_FractionLOD;
    float3 SectorPivot;
    float3 SectionPivot;
};

//Shader Binding
int _LastLOD, _TerrainSize, _SectorSize, _SectionSize, _BufferOffset;

Texture2D _MainTex, _Normal;

TextureCube _Cubemap;

Texture2DArray _SplatArray, _HeightArray, _TangentArray, _AlbedoArray, _NormalArray;

SamplerState sampler_MainTex, Global_point_clamp_sampler, Global_bilinear_clamp_sampler, Global_trilinear_clamp_sampler, Global_point_repeat_sampler, Global_bilinear_repeat_sampler, Global_trilinear_repeat_sampler;

StructuredBuffer<FSectionData> _TerrainBuffer;


//Deprecated
float2 MorphVertex(int NumQuad, int QuadScale, float MorphValue, float2 UV, float2 VertexPos_WS)
{
    float2 fracPart = frac(UV * NumQuad * 0.5) * 2 * rcp(NumQuad);
    fracPart *= QuadScale * MorphValue;

    float2 TLMorph = VertexPos_WS - float2(-fracPart.x, fracPart.y);
    float2 TRMorph = VertexPos_WS - float2(fracPart.x, fracPart.y);
    float2 BLMorph = VertexPos_WS + float2(fracPart.x, fracPart.y);
    float2 BRMorph = VertexPos_WS + float2(-fracPart.x, fracPart.y); 

    float2 LerpUV = frac( UV * (NumQuad * 0.25) );
    float TopAlpha = step(LerpUV.x, 0.5);
    float ButtomAlpha = step(LerpUV.y, 0.5);
    float2 TotPos = lerp(TLMorph, TRMorph, TopAlpha);
    float2 ButtomPos = lerp(BLMorph, BRMorph, TopAlpha);

    return lerp(ButtomPos, TotPos, ButtomAlpha);
}

float4 Texture2DSampleLevel_Bilinear(Texture2D Texture, SamplerState TextureSample, float LOD, float2 UV, float TexelSize, float TextureSize)
{
    float4 TopLeft = Texture.SampleLevel(TextureSample, UV, LOD, 0);
    float4 TopRight = Texture.SampleLevel(TextureSample, UV + float2(TexelSize, 0), LOD, 0);
    float4 ButtomLeft = Texture.SampleLevel(TextureSample, UV + float2(0, TexelSize), LOD, 0);
    float4 ButtomRight = Texture.SampleLevel(TextureSample, UV + float2(TexelSize, TexelSize), LOD, 0);

    float2 InterpolLUT = frac(UV * TextureSize); 
    float4 Top = lerp(TopLeft, TopRight, InterpolLUT.x);
    float4 Buttom = lerp(ButtomLeft, ButtomRight, InterpolLUT.x);

    return lerp(Top, Buttom, InterpolLUT.y); 
}


//Terrain Corrd
struct FTerrainCorrd
{
    float2 QuadUV;
    float2 CoordUV;
    float2 SectorUV;
    float2 HalfCoordUV;
    float2 SectionUV;
};

void InitTerrainCorrd(inout FTerrainCorrd TerrainCorrd, int TerrainSize, int SectorSize, int SectionSize, int NumQuad, float3 SectorCenter, float3 SectionCenter, float3 WorldPosition)
{
    TerrainCorrd.SectorUV = (WorldPosition.xz - SectorCenter.xz) * rcp(TerrainSize);
    TerrainCorrd.SectionUV = TerrainCorrd.SectorUV * SectorSize;
    TerrainCorrd.QuadUV = TerrainCorrd.SectionUV * (NumQuad * 0.5);
    TerrainCorrd.CoordUV = TerrainCorrd.SectionUV * SectionSize;
    TerrainCorrd.HalfCoordUV = TerrainCorrd.SectionUV * SectionSize * 0.5;
}


//Terrain Morph
float GetSectionLod(float LastLOD, float FractionLOD)
{
    return min(FractionLOD, LastLOD);
}

float GetNeighborLod(float LastLOD, float FractionLOD, float NeighborFraction)
{	
    return min( max( NeighborFraction, GetSectionLod(LastLOD, FractionLOD) ), LastLOD );
}

float GetLODCalculated(float2 UV, FSectionData SectionData)
{
    float4 L0 = float4(UV.y, UV.x, (1 - UV.x), (1 - UV.y)) * 2;
    float4 NeighborLOD = float4( GetNeighborLod(_LastLOD, SectionData.FractionLOD, SectionData.Left_FractionLOD), GetNeighborLod(_LastLOD, SectionData.FractionLOD, SectionData.Buttom_FractionLOD), GetNeighborLod(_LastLOD, SectionData.FractionLOD, SectionData.Top_FractionLOD), GetNeighborLod(_LastLOD, SectionData.FractionLOD, SectionData.Right_FractionLOD) );
    float4 LODCalculated4 = L0 * GetSectionLod(_LastLOD, SectionData.FractionLOD) + (1 - L0) * NeighborLOD;
    return ((UV.x + UV.y) > 1) ? (UV.x < UV.y ? LODCalculated4.w : LODCalculated4.z) : (UV.x < UV.y ? LODCalculated4.y : LODCalculated4.x);
}

float3 MorphVertex(in float2 UV, FSectionData SectionData, in float3 VertexPosition)
{
    float LODCalculated = GetLODCalculated(UV, SectionData);
    float LodValue = floor(LODCalculated);
    float MorphAlpha = LODCalculated - LodValue;

    // MorphPosition
    float3 LocalPosition = VertexPosition;
    float2 ActualLODCoordsInt = floor((UV * (SectionData.NumQuad - 1)) * pow(2, -(LodValue - SectionData.LODIndex)));
    float InvLODScaleFactor = pow(2, -LodValue);
    float2 CoordTranslate = float2(_SectionSize * InvLODScaleFactor - 1, max(_SectionSize * 0.5 * InvLODScaleFactor, 2) - 1) * rcp(_SectionSize);
    float2 InputPositionLODAdjusted = ActualLODCoordsInt / CoordTranslate.x;
    float2 InputPositionNextLOD = (floor(ActualLODCoordsInt * 0.5) / CoordTranslate.y);
    LocalPosition.xz = lerp(float2(InputPositionLODAdjusted), float2(InputPositionNextLOD), MorphAlpha);

    float2 OldUV = InputPositionLODAdjusted + SectionData.SectionPivot.xz;
    OldUV = (OldUV - SectionData.SectorPivot.xz) * rcp(_TerrainSize);
    float OldHeight = _HeightArray.SampleLevel(Global_bilinear_clamp_sampler, float3(OldUV, SectionData.HeightmapIndex), 0, 0).r;
    float2 NewUV = InputPositionNextLOD + SectionData.SectionPivot.xz;
    NewUV = (NewUV - SectionData.SectorPivot.xz) * rcp(_TerrainSize);
    float NewHeight = _HeightArray.SampleLevel(Global_bilinear_clamp_sampler, float3(NewUV, SectionData.HeightmapIndex), 0, 0).r;
    float Height = lerp(OldHeight, NewHeight, MorphAlpha);

    float3 WorldPosition = LocalPosition.xyz + SectionData.SectionPivot.xyz;
    WorldPosition.y += UnpackHeightmap(Height) * SectionData.ScaleY;

    /*float2 SectorUV = (WorldPosition.xz - SectionData.SectorPivot.xz) * rcp(_TerrainSize);
    float4 Height = _HeightArray.SampleLevel(Global_point_clamp_sampler, float3(SectorUV, SectionData.HeightmapIndex), 0, 0);
    WorldPosition.y += UnpackHeightmap(Height) * SectionData.ScaleY;*/

    return WorldPosition;
}   


//MipLevel 
float MipLevel(float2 UV)
{
    float2 DX = ddx(UV);
    float2 DY = ddy(UV);
    float MaxSqr = max(dot(DX, DX), dot(DY, DY));
    float MipLevel = 0.5 * log2(MaxSqr);
    return max(0, MipLevel);
}

float MipLevelAniso2D(float2 UV, float MaxAnisoLog2)
{
    float2 DX = ddx(UV);
    float2 DY = ddy(UV);
	float PX = dot(DX, DX);
	float PY = dot(DY, DY);
	float MinLevel = 0.5f * log2(min(PX, PY));
	float MaxLevel = 0.5f * log2(max(PX, PY));
	float AnisoBias = min(MaxLevel - MinLevel, MaxAnisoLog2);
	return MaxLevel - AnisoBias;
}


//Data Struct
struct FSurfaceTexture
{
    float3 Albedo;
    float4 Normal;
};

//Terrain TextureLayer
/*FSurfaceTexture SampleSurfaceTexture(FTerrainCorrd TerrainCorrd, FSectionData SectionData)
{
    FSurfaceTexture OutData;
    OutData.Albedo = 0;
    OutData.Normal = 0;

    int4 SurfaceIndexBuffer[4] = {SectionData.SurfacemapAIndex, SectionData.SurfacemapBIndex, SectionData.SurfacemapCIndex, SectionData.SurfacemapDIndex};
    float SectorMip = MipLevel(TerrainCorrd.SectorUV * _TerrainSize);
    float SurfaceMip = MipLevel(TerrainCorrd.CoordUV * 256);

    [unroll(4)]
    for(int i = 0; i < SectionData.SplatmapCount; i++)
    {
        int4 SurfacemapIndex = SurfaceIndexBuffer[i];
        float4 Splatmap = _SplatArray.SampleLevel(Global_trilinear_clamp_sampler, float3(TerrainCorrd.SectorUV, SectionData.SplatmapIndex + i), SectorMip);

        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.x), SurfaceMip).rgb * Splatmap.r;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.y), SurfaceMip).rgb * Splatmap.g;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.z), SurfaceMip).rgb * Splatmap.b;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.w), SurfaceMip).rgb * Splatmap.a;

        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.x), SurfaceMip) * Splatmap.r;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.y), SurfaceMip) * Splatmap.g;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.z), SurfaceMip) * Splatmap.b;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.w), SurfaceMip) * Splatmap.a;
    }

    return OutData;
}*/

FSurfaceTexture SampleSurfaceTexture(FTerrainCorrd TerrainCorrd, FSectionData SectionData)
{
    FSurfaceTexture OutData;
    OutData.Albedo = 0;
    OutData.Normal = 0;

    int4 SurfaceIndexBuffer[2] = { SectionData.SurfacemapAIndex, SectionData.SurfacemapBIndex };
    float SectorMip = MipLevel(TerrainCorrd.SectorUV * _TerrainSize);
    float SurfaceMip = MipLevel(TerrainCorrd.CoordUV * 256);

    float2 SplatUV = (TerrainCorrd.SectorUV * (_TerrainSize - 1.0f) + 0.5f) * rcp(_TerrainSize);

    [unroll(4)]
    for (int i = 0; i < SectionData.SplatmapCount; i++)
    {
        int4 SurfacemapIndex = SurfaceIndexBuffer[i];
        float4 Splatmap = _SplatArray.SampleLevel(Global_trilinear_clamp_sampler, float3(SplatUV, SectionData.SplatmapIndex + i), SectorMip);

        //OutData.Albedo += Splatmap.rgb;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.x), SurfaceMip).rgb * Splatmap.r;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.y), SurfaceMip).rgb * Splatmap.g;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.z), SurfaceMip).rgb * Splatmap.b;
        OutData.Albedo += _AlbedoArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.w), SurfaceMip).rgb * Splatmap.a;

        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.x), SurfaceMip) * Splatmap.r;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.y), SurfaceMip) * Splatmap.g;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.z), SurfaceMip) * Splatmap.b;
        OutData.Normal += _NormalArray.SampleLevel(Global_trilinear_repeat_sampler, float3(TerrainCorrd.CoordUV, SurfacemapIndex.w), SurfaceMip) * Splatmap.a;
    }

    return OutData;
}

#endif