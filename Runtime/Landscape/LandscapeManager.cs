using UnityEngine;
using Unity.Mathematics;
using Landscape.Terrain;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Landscape.ProceduralVirtualTexture;
using UnityEngine.Experimental.Rendering;

namespace Landscape.Terrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FTerrainBatch
    {
        public int NumQuad;
        public int LODIndex;

        public int HeightmapIndex;
        public int SplatmapIndex;
        public int SplatmapCount;
        public int SurfacemapCount;
        public int4 SurfacemapAIndex;
        public int4 SurfacemapBIndex;
        //public int4 SurfacemapCIndex;
        //public int4 SurfacemapDIndex;

        public float ScaleY;

        public float FractionLOD;
        public float Top_FractionLOD;
        public float Buttom_FractionLOD;
        public float Left_FractionLOD;
        public float Right_FractionLOD;

        public float3 SectorPivot;
        public float3 SectionPivot;
    }

    public class FTerrainBuffer
    {
        protected ComputeBuffer TerrainBuffer;

        public FTerrainBuffer()
        {

        }

        ~FTerrainBuffer()
        {

        }

        public void CreateBuffer()
        {
            int BitSize = Marshal.SizeOf(typeof(FTerrainBatch));
            TerrainBuffer = new ComputeBuffer(1024, BitSize);
            TerrainBuffer.name = "TerrainBuffer";
        }

        public ComputeBuffer GetBuffer()
        {
            return TerrainBuffer;
        }

        public void SetBufferData(int OffsetIndex, int CopyCount, List<FTerrainBatch> MeshBatchData)
        {
            TerrainBuffer.SetData<FTerrainBatch>(MeshBatchData, 0, OffsetIndex, CopyCount);
        }

        public void ReleaseBuffer()
        {
            TerrainBuffer.Release();
            TerrainBuffer.Dispose();
        }
    }

    public struct FTerrainBatchInitializer
    {
        public int SplatmapCount;
        public int SplatmapIndex;
        public int HeightmapIndex;
        public int SurfacemapCount;
        public float4x4 MatrixProj;
        public Plane[] FrustumPlane;
        public TerrainLayer[] TerrainLayer;
    }

    public class FTerrainBatchCollector
    {
        public List<FTerrainBatch>[] MeshBatch;

        public FTerrainBatchCollector()
        {

        }

        ~FTerrainBatchCollector()
        {

        }

        public void CreateCollection()
        {
            MeshBatch = new List<FTerrainBatch>[7];

            MeshBatch[0] = new List<FTerrainBatch>(16);
            MeshBatch[1] = new List<FTerrainBatch>(128);
            MeshBatch[2] = new List<FTerrainBatch>(256);
            MeshBatch[3] = new List<FTerrainBatch>(512);
            MeshBatch[4] = new List<FTerrainBatch>(512);
            MeshBatch[5] = new List<FTerrainBatch>(1024);
            MeshBatch[6] = new List<FTerrainBatch>(1024);
        }

        public void ResetCollection()
        {
            MeshBatch[0].Clear();
            MeshBatch[1].Clear();
            MeshBatch[2].Clear();
            MeshBatch[3].Clear();
            MeshBatch[4].Clear();
            MeshBatch[5].Clear();
            MeshBatch[6].Clear();
        }

        public List<FTerrainBatch> GetTerrainBatch(int LODIndex)
        {
            return MeshBatch[LODIndex];
        }

        public void AddTerrainBatch(int LODIndex, FTerrainBatch InMeshBatch)
        {
            switch (LODIndex)
            {
                case 0:
                    {
                        MeshBatch[0].Add(InMeshBatch);
                        break;
                    }

                case 1:
                    {
                        MeshBatch[1].Add(InMeshBatch);
                        break;
                    }

                case 2:
                    {
                        MeshBatch[2].Add(InMeshBatch);
                        break;
                    }

                case 3:
                    {
                        MeshBatch[3].Add(InMeshBatch);
                        break;
                    }

                case 4:
                    {
                        MeshBatch[4].Add(InMeshBatch);
                        break;
                    }

                case 5:
                    {
                        MeshBatch[5].Add(InMeshBatch);
                        break;
                    }

                case 6:
                    {
                        MeshBatch[6].Add(InMeshBatch);
                        break;
                    }
            }
        }

        public void ReleaseCollection()
        {
            MeshBatch = null;
        }
    }

    public struct FTerrainDrawCommand
    {
        public int InstanceCount;
        public int BufferOffset;

        public FTerrainDrawCommand(in int Count, in int Offset)
        {
            InstanceCount = Count;
            BufferOffset = Offset;
        }
    }
}

namespace Landscape
{
    [AddComponentMenu("Landscape/LandscapeManager")]
    public class LandscapeManager : MonoBehaviour
    {
        public static bool bStartRender = false;
        public static bool bCopyTexture = false;

        public static RenderTexture HeightArray;
        public static RenderTexture TangentArray;
        public static Texture2DArray SplatArray;
        public static Texture2DArray AlbedoArray;
        public static Texture2DArray NormalArray;

        public static FTerrainBuffer TerrainBuffer;
        public static FTerrainBatchCollector TerrainBatchCollector;

        public static VirtualTextureVolume VTVolumeProxy;
        protected static List<TerrainComponent> LandscapeProxyList = new List<TerrainComponent>();


        [Header("TextureSetting")]
        public int TextureSize = 1024;
        public int TextureSlice = 4;

        public TerrainLayer[] TextureLayer;
        public static Dictionary<TerrainLayer, int> TextureLayerMap;

        public RuntimeVirtualTexture[] VirtualTexture;
        public static Dictionary<RuntimeVirtualTexture, int> VirtualTextureMap;

        public static Mesh[] TerrainMeshs;
        public static Material[] TerrainMaterials;


        void OnEnable()
        {
            //Init State
            bStartRender = true;
            bCopyTexture = true;


            CreateTexture();
            InitializeTexture();

            CreateMeshBatch();
            CreateMeshElement();
        }

        void Update()
        {

        }

        void OnDisable()
        {
            //Release State
            bStartRender = false;
            bCopyTexture = false;

            //Release MeshBatch
            TerrainBuffer.ReleaseBuffer();
            TerrainBatchCollector.ReleaseCollection();
            TerrainBuffer = null;
            TerrainBatchCollector = null;

            //Release Texture
            RenderTexture.ReleaseTemporary(HeightArray);
            RenderTexture.ReleaseTemporary(TangentArray);
            GameObject.DestroyImmediate(NormalArray);
            GameObject.DestroyImmediate(AlbedoArray);
            GameObject.DestroyImmediate(SplatArray);

            //Release LayerMap
            TextureLayerMap.Clear();
            VirtualTextureMap.Clear();
        }

        private void CreateMeshBatch()
        {
            TerrainBuffer = new FTerrainBuffer();
            TerrainBuffer.CreateBuffer();

            TerrainBatchCollector = new FTerrainBatchCollector();
            TerrainBatchCollector.CreateCollection();
        }

        private void CreateMeshElement()
        {
            //Create TerrainMaterial
            TerrainMaterials = new Material[7];
            for (int i = 0; i < 7; ++i)
            {
                TerrainMaterials[i] = new Material(Shader.Find("Terrain/TerrainLit_Instance"));
            }

            //Create TerrainMesh
            List<Mesh> TerrainQuadMesh = new List<Mesh>();
            for (int NumQuad = 64; NumQuad > 0;)
            {
                TerrainQuadMesh.Add(TerrainMesh.BuildSectionMesh(false, NumQuad, 64));
                NumQuad >>= 1;
            }
            TerrainMeshs = new Mesh[TerrainQuadMesh.Count];
            TerrainMeshs = TerrainQuadMesh.ToArray();
        }

        private void CreateTexture()
        {
            RenderTextureDescriptor HeightTextureDesc = new RenderTextureDescriptor { width = TextureSize + 1, height = TextureSize + 1, volumeDepth = TextureSlice, dimension = TextureDimension.Tex2DArray, graphicsFormat = GetHeightmapFormat(SystemInfo.graphicsDeviceType), depthBufferBits = 0, mipCount = -1, useMipMap = false, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
            HeightArray = RenderTexture.GetTemporary(HeightTextureDesc);
            HeightArray.name = "TerrainHeightTextureArray";

            RenderTextureDescriptor NormalTextureDesc = new RenderTextureDescriptor { width = TextureSize + 1, height = TextureSize + 1, volumeDepth = TextureSlice, dimension = TextureDimension.Tex2DArray, graphicsFormat = GraphicsFormat.A2B10G10R10_UNormPack32, depthBufferBits = 0, mipCount = 11, useMipMap = true, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
            TangentArray = RenderTexture.GetTemporary(NormalTextureDesc);
            TangentArray.name = "TerrainTangentTextureArray";

            SplatArray = new Texture2DArray(TextureSize, TextureSize, TextureSlice, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.MipChain);
            SplatArray.name = "TerrainSplatTextureArray";

            AlbedoArray = new Texture2DArray(256, 256, TextureLayer.Length, GetAlbedoFormat(), TextureCreationFlags.MipChain);
            AlbedoArray.name = "TerrainAlbedoTextureArray";

            NormalArray = new Texture2DArray(256, 256, TextureLayer.Length, GetNormalFormat(), TextureCreationFlags.MipChain);
            NormalArray.name = "TerrainNormalTextureArray";

            TextureLayerMap = new Dictionary<TerrainLayer, int>();
            VirtualTextureMap = new Dictionary<RuntimeVirtualTexture, int>();
        }

        private void InitializeTexture()
        {
            for (int i = 0; i < TextureLayer.Length; i++)
            {
                Graphics.CopyTexture(TextureLayer[i].diffuseTexture, 0, AlbedoArray, i);
                Graphics.CopyTexture(TextureLayer[i].normalMapTexture, 0, NormalArray, i);
            }

            for (int i = 0; i < TextureLayer.Length; i++)
            {
                TextureLayerMap.Add(TextureLayer[i], i);
            }

            for (int i = 0; i < VirtualTexture.Length; i++)
            {
                VirtualTextureMap.Add(VirtualTexture[i], i);
            }
        }

        private GraphicsFormat GetAlbedoFormat()
        {
            //return GraphicsFormat.R8G8B8A8_SRGB;
            return TextureLayer[0].diffuseTexture.graphicsFormat;
        }

        private GraphicsFormat GetNormalFormat()
        {
            //return GraphicsFormat.R8G8B8A8_UNorm;
            return TextureLayer[0].normalMapTexture.graphicsFormat;
        }

        private GraphicsFormat GetHeightmapFormat(GraphicsDeviceType Platform)
        {
            GraphicsFormat OutputFormat = GraphicsFormat.R16_UNorm;

            switch (Platform)
            {
                case GraphicsDeviceType.Metal:
                    {
                        OutputFormat = GraphicsFormat.R8G8_UNorm;
                        break;
                    }

                case (GraphicsDeviceType.Vulkan):
                    {
                        OutputFormat = GraphicsFormat.R8G8_UNorm;
                        break;
                    }

                case (GraphicsDeviceType.OpenGLES3):
                    {
                        OutputFormat = GraphicsFormat.R8G8_UNorm;
                        break;
                    }

                case (GraphicsDeviceType.OpenGLCore):
                    {
                        OutputFormat = GraphicsFormat.R16_UNorm;
                        break;
                    }

                case (GraphicsDeviceType.Direct3D12):
                    {
                        OutputFormat = GraphicsFormat.R16_UNorm;
                        break;
                    }

                case (GraphicsDeviceType.Direct3D11):
                    {
                        OutputFormat = GraphicsFormat.R16_UNorm;
                        break;
                    }
            }

            return OutputFormat;
        }

        public static int GetTerrainLayerID(TerrainLayer InTerrainLayer)
        {
            int OutIndex = 0;
            if (TextureLayerMap.TryGetValue(InTerrainLayer, out OutIndex)) { }

            return OutIndex;
        }

        public static int GetVirtualTextureID(RuntimeVirtualTexture InVirtualTexture)
        {
            int OutIndex = 0;
            if (VirtualTextureMap.TryGetValue(InVirtualTexture, out OutIndex)) { }

            return OutIndex;
        }

        //
        public static void AddLandscapeProxy(TerrainComponent InLandscapeProxy)
        {
            LandscapeProxyList.Add(InLandscapeProxy);
        }

        public static void RemoveLandscapeProxy(TerrainComponent InLandscapeProxy)
        {
            LandscapeProxyList.Remove(InLandscapeProxy);
        }

        public static List<TerrainComponent> GetLandscapeProxyList()
        {
            return LandscapeProxyList;
        }
    }
}
