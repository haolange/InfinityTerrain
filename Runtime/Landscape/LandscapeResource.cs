using UnityEngine;
using Landscape.Terrain;
using System.Collections.Generic;

namespace Landscape
{
    [CreateAssetMenu(menuName = "Landscape/ResourceProfile")]
    public class LandscapeResource : ScriptableObject
    {
#if UNITY_EDITOR
        [Header("Icon")]
        public Texture2D CreateIcon;
        public Texture2D ImportIcon;
        public Texture2D ExportIcon;
        public Texture2D SculptIcon;
        public Texture2D EraseIcon;
        public Texture2D SmoothIcon;
        public Texture2D FlattenIcon;
#endif

        [Header("TerrainData")]
        public Material TerrainMaterial;
        //[HideInInspector]
        public TerrainVertexData[] TerrainMeshs;


        public LandscapeResource()
        {
            BuildTerrainMesh();
        }

        void OnEnable()
        {

        }

        void OnValidate()
        {

        }

        void OnDisable()
        {

        }

        void OnDestroy()
        {
            TerrainMeshs = null;
        }

        void BuildTerrainMesh()
        {
            // Build TerrainMeshes
            List<TerrainVertexData> TerrainQuadMesh = new List<TerrainVertexData>();

            uint LODIndex = 0;
            for (int NumQuad = 64; NumQuad > 0;)
            {
                TerrainVertexData LODMesh = TerrainMesh.BuildSectionVertexData(false, NumQuad, 64);
                LODMesh.name = "TerrainMesh_LOD" + LODIndex.ToString();
                TerrainQuadMesh.Add(LODMesh);

                LODIndex += 1;
                NumQuad >>= 1;
            }
            TerrainMeshs = new TerrainVertexData[TerrainQuadMesh.Count];
            TerrainMeshs = TerrainQuadMesh.ToArray();
        }
    }
}
