using UnityEngine;
using Landscape.Utils;

namespace Landscape.Terrain
{
    public class TerrainTexture 
    {
        public Texture2D HeightMap;

        public TerrainTexture(int TextureSize)
        {
            HeightMap = new Texture2D(TextureSize, TextureSize, TextureFormat.R16, false, true);
        }

        public void TerrainDataToHeightmap(TerrainData InTerrainData)
        {
            if (HeightMap.width != 0)
                HeightmapLoader.TerrainDataToTexture(HeightMap, InTerrainData);
        }

        public void Release()
        {
            HeightMap = null;
        }
    }
}
