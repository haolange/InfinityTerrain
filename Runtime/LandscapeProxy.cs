using UnityEngine;
using Landscape.Utils;
using Landscape.Terrain;
using System.Runtime.CompilerServices;
using Landscape.ProceduralVirtualTexture;

namespace Landscape
{
    //[ExecuteInEditMode]
    [RequireComponent(typeof(UnityEngine.Terrain))]
    [AddComponentMenu("Landscape/LandscapeProxy")]
    public class LandscapeProxy : MonoBehaviour
    {
        [HideInInspector]
        public LandscapeResource ResourceProfile;

        #region TerrainSector
        [Header("LOD Setting")]
        public float LOD0ScreenSize = 0.5f;
        public float LOD0Distribution = 1.25f;
        public float LODDistribution = 2.8f;

        [Header("VirtualTexture")]
        public RuntimeVirtualTexture VirtualTexture;

        [HideInInspector]
        public UnityEngine.Terrain UnityTerrain;
        [HideInInspector]
        public TerrainTexture TextureData;
        [HideInInspector]
        public TerrainData UnityTerrainData;

        [HideInInspector]
        public int TerrainSize;
        [HideInInspector]
        public float TerrainScaleY;
        [HideInInspector]
        public int TerrainSectorSize;
        [HideInInspector]
        public int TerrainSectionSize;
        [HideInInspector]
        public TerrainSector TerrainSector;
        #endregion


        public LandscapeProxy()
        {

        }

        void OnEnable() 
        {
            ResourceProfile = Resources.Load<LandscapeResource>("LandscapeResourceProfile");

            InitTerrain();
            AddWorldLandscape();
        }

        void Start()
        {

        }

        void Update()
        {

        }

        void OnDisable() 
        {
            ResourceProfile = null;
            RemoveWorldLandscape();
        }

        // TerrainSector
        #region TerrainSector
        public void InitTerrain()
        {
            UnityTerrain = GetComponent<UnityEngine.Terrain>();
            UnityTerrainData = GetComponent<TerrainCollider>().terrainData;
            UnityTerrain.drawHeightmap = false;

            TerrainSector.InitSections(TerrainSectorSize, 7, LOD0ScreenSize, LOD0Distribution, LODDistribution);
        }

        public void SerializeTerrain()
        {
            UnityTerrain = GetComponent<UnityEngine.Terrain>();
            UnityTerrainData = GetComponent<TerrainCollider>().terrainData;

            TerrainSize = UnityTerrainData.heightmapResolution - 1;
            TerrainScaleY = UnityTerrainData.size.y;
            TerrainSectorSize = LandscapeUtility.GetSectionNumFromTerrainSize(TerrainSize);
            TerrainSectionSize = (TerrainSize) / LandscapeUtility.GetSectionNumFromTerrainSize(TerrainSize);

            TextureData = new TerrainTexture(TerrainSize);
            TextureData.TerrainDataToHeightmap(UnityTerrainData);

            TerrainSector = new TerrainSector();
            TerrainSector.Serialize(TerrainSize, TerrainSectorSize, TerrainSectionSize, transform.position, UnityTerrainData.bounds);
            TerrainSector.CorrectionBounds(TerrainSectionSize, TerrainSize, TerrainScaleY, transform.position, TextureData.HeightMap);

            TextureData.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTexture GetNormalTexture()
        {
            return UnityTerrain.normalmapTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RenderTexture GetHeightTexture()
        {
            return UnityTerrainData.heightmapTexture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2D GetSplatTexture(int SplatIndex)
        {
            return UnityTerrainData.alphamapTextures[SplatIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Terrain GetTerrain()
        {
            return UnityTerrain;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TerrainData GetTerrainData()
        {
            return UnityTerrainData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TerrainLayer[] GetTerrainLayer()
        {
            return UnityTerrainData.terrainLayers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTerrainBatch(Camera RenderCamera, FTerrainBatchCollector TerrainBatchCollector, in FTerrainBatchInitializer TerrainBatchInitializer)
        {
            TerrainSector.GetTerrainBatch(TerrainSectionSize, TerrainScaleY, transform.position, RenderCamera.transform.position, TerrainBatchCollector, TerrainBatchInitializer);
        }
        #endregion


        // LandscapeProxy
        #region LandscapeProxy
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bounds GetBounds()
        {
            Vector3 Position = transform.position;
            Bounds BoundinBox = GetComponent<TerrainCollider>().terrainData.bounds;
            int SectorSize_Half = TerrainSize / 2;

            return new Bounds(new Vector3(Position.x + SectorSize_Half, Position.y + (BoundinBox.size.y / 2), Position.z + SectorSize_Half), BoundinBox.size);
        }

        private void AddWorldLandscape()
        {
            LandscapeManager.AddLandscapeProxy(this);
        }

        private void RemoveWorldLandscape()
        {
            LandscapeManager.RemoveLandscapeProxy(this);
        }
        #endregion
    }
}
