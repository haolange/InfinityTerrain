using System;
using UnityEngine;
using Landscape.Utils;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Landscape.Terrain
{
    public struct SectionLODData
	{
        public int LastLODIndex;
        public float LOD0ScreenSizeSquared;
		public float LOD1ScreenSizeSquared;
		public float LODOnePlusDistributionScalarSquared;
		public float LastLODScreenSizeSquared;
	};


    [Serializable]
    public class TerrainSection
    {
        public int NumQuad;
        public int LODIndex;
        public float FractionLOD;

        public Bounds BoundinBox;
        public Vector3 PivotPosition;
        public Vector3 CenterPosition;

        private SectionLODData LODSettings;

        [NonSerialized]
        public TerrainSection[] NeighborSection;


        public TerrainSection()
        {
            NeighborSection = new TerrainSection[4];
        }

        public void Init(int InMaxLOD, float InLOD0ScreenSize, float InLOD0Distribution, float InLODDistribution) 
        {
            float[] LODScreenRatioSquared = new float[InMaxLOD];

            float CurrentScreenSizeRatio = InLOD0ScreenSize;
            float ScreenSizeRatioDivider = Mathf.Max(InLOD0Distribution, 1.01f);
            LODScreenRatioSquared[0] = LandscapeUtility.Squared(CurrentScreenSizeRatio);

            // LOD 0 handling
            LODSettings.LOD0ScreenSizeSquared = LandscapeUtility.Squared(CurrentScreenSizeRatio);
            CurrentScreenSizeRatio /= ScreenSizeRatioDivider;
            LODSettings.LOD1ScreenSizeSquared = LandscapeUtility.Squared(CurrentScreenSizeRatio);
            ScreenSizeRatioDivider = Mathf.Max(InLODDistribution, 1.01f);
            LODSettings.LODOnePlusDistributionScalarSquared = ScreenSizeRatioDivider * ScreenSizeRatioDivider;

            // Other LODs
            for (int LOD_Index = 1; LOD_Index <= InMaxLOD - 1; ++LOD_Index) // This should ALWAYS be calculated from the component size, not user MaxLOD override
            {
                LODScreenRatioSquared[LOD_Index] = LandscapeUtility.Squared(CurrentScreenSizeRatio);
                CurrentScreenSizeRatio /= ScreenSizeRatioDivider;
            }

            // Clamp ForcedLOD to the valid range and then apply
            LODSettings.LastLODIndex = InMaxLOD;
            LODSettings.LastLODScreenSizeSquared = LODScreenRatioSquared[InMaxLOD - 1];
        }

        private int GetSurfacemapID(TerrainLayer[] InTerrainLayer, int Index)
        {
            int OutIndex = -1;

            if(Index <= InTerrainLayer.Length - 1) {
                OutIndex = LandscapeManager.GetTerrainLayerID(InTerrainLayer[Index]);
            }

            return OutIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTerrainBatch(int SectionSize, float TerrainScaleY, float3 SectorPosition, float3 ViewOringin, FTerrainBatchCollector TerrainBatchCollector, in FTerrainBatchInitializer TerrainBatchInitializer) 
        {
            float ScreenSize = LandscapeUtility.ComputeBoundsScreenRadiusSquared(LandscapeUtility.GetBoundRadius(BoundinBox), BoundinBox.center, ViewOringin, TerrainBatchInitializer.MatrixProj);
            LODIndex = math.min(6, LandscapeUtility.GetLODFromScreenSize(LODSettings, ScreenSize, 1, out FractionLOD));
            FractionLOD = math.min(5, FractionLOD);
            NumQuad = Mathf.Clamp(SectionSize >> LODIndex, 1, SectionSize);

            TerrainLayer[] TerrainLayer = TerrainBatchInitializer.TerrainLayer;

            FTerrainBatch ShaderParameter;
            {
                ShaderParameter.NumQuad = math.max(2, NumQuad);
                ShaderParameter.LODIndex = math.min(5, LODIndex);
                ShaderParameter.ScaleY = TerrainScaleY * 2;
                ShaderParameter.SplatmapCount = TerrainBatchInitializer.SplatmapCount;
                ShaderParameter.SplatmapIndex = TerrainBatchInitializer.SplatmapIndex;
                ShaderParameter.HeightmapIndex = TerrainBatchInitializer.HeightmapIndex;
                ShaderParameter.SurfacemapCount = TerrainBatchInitializer.SurfacemapCount;
                ShaderParameter.FractionLOD = FractionLOD;
                ShaderParameter.Top_FractionLOD = NeighborSection[0] != null ? NeighborSection[0].FractionLOD : FractionLOD;
                ShaderParameter.Buttom_FractionLOD = NeighborSection[1] != null ? NeighborSection[1].FractionLOD : FractionLOD;
                ShaderParameter.Left_FractionLOD = NeighborSection[2] != null ? NeighborSection[2].FractionLOD : FractionLOD;
                ShaderParameter.Right_FractionLOD = NeighborSection[3] != null ? NeighborSection[3].FractionLOD : FractionLOD;
                ShaderParameter.SectorPivot = SectorPosition;
                ShaderParameter.SectionPivot = PivotPosition;

                ShaderParameter.SurfacemapAIndex = new int4(GetSurfacemapID(TerrainLayer, 0), GetSurfacemapID(TerrainLayer, 1), GetSurfacemapID(TerrainLayer, 2), GetSurfacemapID(TerrainLayer, 3));
                ShaderParameter.SurfacemapBIndex = new int4(GetSurfacemapID(TerrainLayer, 4), GetSurfacemapID(TerrainLayer, 5), GetSurfacemapID(TerrainLayer, 6), GetSurfacemapID(TerrainLayer, 7));
                //ShaderParameter.SurfacemapCIndex = new int4(GetSurfacemapID(TerrainLayer, 8), GetSurfacemapID(TerrainLayer, 9), GetSurfacemapID(TerrainLayer, 10), GetSurfacemapID(TerrainLayer, 11));
                //ShaderParameter.SurfacemapDIndex = new int4(GetSurfacemapID(TerrainLayer, 12), GetSurfacemapID(TerrainLayer, 13), GetSurfacemapID(TerrainLayer, 14), GetSurfacemapID(TerrainLayer, 15));
            }
            TerrainBatchCollector.AddTerrainBatch(LODIndex, ShaderParameter);
        }    
    }
}



















































//Debug.Log(SectionName + " : _LODIndex_ : " + LODIndex);
//Debug.Log(SectionName + " : _FractionLOD_ : " + FractionLOD);

/*for(int i = 0; i <= NeighborSection.Length - 1; i++) 
{
    if(NeighborSection[i] != null) {
        Debug.Log("_Slef : " + SectionName + "_FractionLOD : " + FractionLOD + "    _Neighbor : " + NeighborSection[i].SectionName + "_FractionLOD : " + NeighborSection[i].FractionLOD);
    } else {
        Debug.Log("_Slef : " + SectionName + "_FractionLOD : null");
    }
}*/