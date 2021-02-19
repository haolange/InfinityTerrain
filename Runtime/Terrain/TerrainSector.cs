using System;
using UnityEngine;
using Landscape.Utils;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Landscape.Terrain
{
    [Serializable]
    public class TerrainSector
    {
        public Bounds BoundinBox;
        public TerrainSection[] TerrainSections;

        public TerrainSector()
        {

        }

        public void Serialize(int SectorSize, int NumSection, int NumQuad, Vector3 InTerrianPosition, Bounds InSectorBound) 
        {
            // Serialize TerrainSection
            int SectorSize_Half = SectorSize / 2;
            int SectionSize_Half = NumQuad / 2;
            Bounds SectorBounds = InSectorBound;
            BoundinBox = new Bounds(new Vector3(InTerrianPosition.x + SectorSize_Half, InTerrianPosition.y + (SectorBounds.size.y / 2), InTerrianPosition.z + SectorSize_Half), SectorBounds.size);

            TerrainSections = new TerrainSection[NumSection * NumSection];

            for (int SectorSizeX = 0; SectorSizeX <= NumSection - 1; SectorSizeX++)
            {
                for (int SectorSizeY = 0; SectorSizeY <= NumSection - 1; SectorSizeY++)
                {
                    int ArrayIndex = (SectorSizeX * NumSection) + SectorSizeY;
                    Vector3 SectionPivotPosition = InTerrianPosition + new Vector3(NumQuad * SectorSizeX, 0, NumQuad * SectorSizeY);
                    Vector3 SectionCenterPosition = SectionPivotPosition + new Vector3(SectionSize_Half, 0, SectionSize_Half);

                    TerrainSections[ArrayIndex] = new TerrainSection();
                    TerrainSections[ArrayIndex].NeighborSection = new TerrainSection[4];

                    TerrainSections[ArrayIndex].PivotPosition = SectionPivotPosition;
                    TerrainSections[ArrayIndex].CenterPosition = SectionCenterPosition;
                    TerrainSections[ArrayIndex].BoundinBox = new Bounds(SectionCenterPosition, new Vector3(NumQuad, 1, NumQuad));
                }
            }
        }

        public void InitSections(int InSectorSize, int InMaxLOD, float InLOD0ScreenSize, float InLOD0Distribution, float InLODDistribution)
        {
            // Search NeighborSection
            foreach (TerrainSection Section in TerrainSections)
            {
                Section.Init(InMaxLOD, InLOD0ScreenSize, InLOD0Distribution, InLODDistribution);
            }

            // Search NeighborSection
            LandscapeUtility.GetNeighborSection(InSectorSize, TerrainSections);
        }

        public void CorrectionBounds(int SectionSize, int TerrainSize, float ScaleY, float3 TerrianPosition, Texture2D Heightmap)
        {
            int TerrainSize_Half = TerrainSize / 2;

            foreach (TerrainSection Section in TerrainSections)
            {
                float2 PositionScale = new float2(TerrianPosition.x, TerrianPosition.z) + new float2(TerrainSize_Half, TerrainSize_Half);
                float2 RectUV = new float2((Section.PivotPosition.x - PositionScale.x) + TerrainSize_Half, (Section.PivotPosition.z - PositionScale.y) + TerrainSize_Half);

                int ReverseScale = TerrainSize - SectionSize;
                Color[] HeightValues = Heightmap.GetPixels(Mathf.FloorToInt(RectUV.x), ReverseScale - Mathf.FloorToInt(RectUV.y), Mathf.FloorToInt(SectionSize), Mathf.FloorToInt(SectionSize), 0);

                float MinHeight = HeightValues[0].r;
                float MaxHeight = HeightValues[0].r;
                for (int i = 0; i < HeightValues.Length; i++)
                {
                    if (MinHeight < HeightValues[i].r)
                    {
                        MinHeight = HeightValues[i].r;
                    }

                    if (MaxHeight > HeightValues[i].r)
                    {
                        MaxHeight = HeightValues[i].r;
                    }
                }

                float PosY = ((Section.CenterPosition.y + MinHeight * ScaleY) + (Section.CenterPosition.y + MaxHeight * ScaleY)) * 0.5f;
                float SizeY = ((Section.CenterPosition.y + MinHeight * ScaleY) - (Section.CenterPosition.y + MaxHeight * ScaleY));
                float3 NewBoundCenter = new float3(Section.CenterPosition.x, PosY, Section.CenterPosition.z);
                Section.BoundinBox = new Bounds(NewBoundCenter, new Vector3(SectionSize, SizeY, SectionSize));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTerrainBatch(int SectionSize, float TerrainScaleY, float3 SectorPosition, float3 ViewOringin, FTerrainBatchCollector TerrainBatchCollector, in FTerrainBatchInitializer TerrainBatchInitializer)
        {
            if (LandscapeUtility.IntersectAABBFrustum(TerrainBatchInitializer.FrustumPlane, BoundinBox))
            {
#if UNITY_EDITOR
                LandscapeUtility.DrawBound(BoundinBox, Color.white);
#endif

                foreach (TerrainSection Section in TerrainSections)
                {
                    if (LandscapeUtility.IntersectAABBFrustum(TerrainBatchInitializer.FrustumPlane, Section.BoundinBox))
                    {
                        //Update MeshBatch
                        Section.GetTerrainBatch(SectionSize, TerrainScaleY, SectorPosition, ViewOringin, TerrainBatchCollector, TerrainBatchInitializer);

#if UNITY_EDITOR
                        //LandscapeUtility.DrawRect(Section.RectBox, Color.red);
                        LandscapeUtility.DrawBound(Section.BoundinBox, new Color(LandscapeUtility.LODColor[Section.LODIndex].x * 0.5f, LandscapeUtility.LODColor[Section.LODIndex].y * 0.5f, LandscapeUtility.LODColor[Section.LODIndex].z * 0.5f));
#endif
                    }
                }
            }
        }
    }
}
