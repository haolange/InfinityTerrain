using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Landscape.ProceduralVirtualTexture
{
    [ExcludeFromPreset]
    [CreateAssetMenu(menuName = "Landscape/VirtualTexture")]
    public class RuntimeVirtualTexture : ScriptableObject
    {
        [Range(16, 32)]
        public int TileBlock = 32;

        [Range(128, 512)]
        public int TileSize = 256;

        [Range(128, 512)]
        public int PageSize = 256;

        [Range(1, 4)]
        public int TileBorder = 1;

        [HideInInspector]
        public int TileSizePadding { get { return TileSize + TileBorder * 2; } }

        [HideInInspector]
        public RenderTexture BufferTextureA;

        [HideInInspector]
        public RenderTexture BufferTextureB;

        [HideInInspector]
        public RenderTexture PageTableTexture;

        [HideInInspector]
        public FTileTexturePool TilePool;


        public RuntimeVirtualTexture()
        {

        }

        public void Initialize()
        {
            int TextureSize = TileBlock * TileSizePadding;

            if (BufferTextureA == null && BufferTextureB == null && PageTableTexture == null)
            {
                RenderTextureDescriptor TextureADesc = new RenderTextureDescriptor { width = TextureSize, height = TextureSize, volumeDepth = 1, dimension = TextureDimension.Tex2D, graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm, depthBufferBits = 0, mipCount = -1, useMipMap = false, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
                BufferTextureA = new RenderTexture(TextureADesc);
                BufferTextureA.name = this.name + "_A";

                RenderTextureDescriptor TextureBDesc = new RenderTextureDescriptor { width = TextureSize, height = TextureSize, volumeDepth = 1, dimension = TextureDimension.Tex2D, graphicsFormat = GraphicsFormat.A2B10G10R10_UIntPack32, depthBufferBits = 0, mipCount = -1, useMipMap = false, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
                BufferTextureB = new RenderTexture(TextureBDesc);
                BufferTextureB.name = this.name + "_B";

                RenderTextureDescriptor PageTableDesc = new RenderTextureDescriptor { width = PageSize, height = PageSize, volumeDepth = 1, dimension = TextureDimension.Tex2D, graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm, depthBufferBits = 0, mipCount = -1, useMipMap = false, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
                PageTableTexture = new RenderTexture(PageTableDesc);
                PageTableTexture.filterMode = FilterMode.Point;
                PageTableTexture.wrapMode = TextureWrapMode.Clamp;
            }

            TilePool = new FTileTexturePool();
            TilePool.Init(TileBlock * TileBlock);
        }

        private int2 IdToPos(int id)
        {
            return new int2(id % TileBlock, id / TileBlock);
        }

        private int PosToId(int2 tile)
        {
            return (tile.y * TileBlock + tile.x);
        }

        public int2 RequestTile()
        {
            return IdToPos(TilePool.First);
        }

        public bool SetActive(int2 tile)
        {
            bool success = TilePool.SetActive(PosToId(tile));

            return success;
        }

        public void Release()
        {
            if (BufferTextureA != null && BufferTextureB != null)
            {
                BufferTextureA.Release();
                BufferTextureB.Release();
            }
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

        }
    }
}