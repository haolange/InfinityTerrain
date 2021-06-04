using UnityEngine;
using Landscape.Utils;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Landscape.ProceduralVirtualTexture
{
    public class VirtualTextureVolume : MonoBehaviour
    {
        [HideInInspector]
        public Bounds BoundBox;

        public int VolumeSize;

        public float PageCellSize
        {
            get
            {
                return 2 * VolumeSize / VirtualTexture.PageSize;
            }
        }

        [HideInInspector]
        public Rect DrawRect;

        public RuntimeVirtualTexture VirtualTexture;
        public List<TerrainComponent> LandscapeProxyList;

        public VirtualTextureVolume()
        {

        }

        void OnEnable()
        {
            VirtualTexture.Initialize();
            LandscapeManager.VTVolumeProxy = this;

            int2 VolumeCenter = GetFixedCenter(GetFixedPosition(transform.position));
            DrawRect = new Rect(VolumeCenter.x - VolumeSize, VolumeCenter.y - VolumeSize, 2 * VolumeSize, 2 * VolumeSize);
        }

        void Update()
        {

        }

        private int2 GetFixedCenter(int2 pos)
        {
            return new int2((int)math.floor(pos.x / VolumeSize + 0.5f) * VolumeSize, (int)math.floor(pos.y / VolumeSize + 0.5f) * VolumeSize);
        }

        private int2 GetFixedPosition(Vector3 pos)
        {
            return new int2((int)math.floor(pos.x / PageCellSize + 0.5f) * (int)PageCellSize, (int)math.floor(pos.z / PageCellSize + 0.5f) * (int)PageCellSize);
        }
#if UNITY_EDITOR
        private void DrawBound()
        {
            BoundBox = new Bounds(transform.position, transform.localScale);
            LandscapeUtility.DrawBound(BoundBox, new Color(0.5f, 1, 0.25f));
        }

        void OnDrawGizmosSelected()
        {
            DrawBound();
        }
#endif

        void OnDisable()
        {
            VirtualTexture.Release();
        }
    }
}
