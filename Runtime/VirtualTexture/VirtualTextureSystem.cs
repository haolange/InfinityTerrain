using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Landscape.Rendering;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Landscape.ProceduralVirtualTexture
{
    public class FFeedbackReader
    {
        public bool bRequest;
        public RenderTexture FeedbackTexture;

        public NativeArray<Color32> ReadbackData;


        public FFeedbackReader(in bool InitState)
        {
            bRequest = InitState;
        }

        public void Execution(CommandBuffer CmdBuffer)
        {
            if (bRequest == true)
            {
                bRequest = false;

                CmdBuffer.RequestAsyncReadback(FeedbackTexture, 0, FeedbackTexture.graphicsFormat, EnqueueCopy);
            }
        }

        public void EnqueueCopy(AsyncGPUReadbackRequest Request)
        {
            if (Request.hasError || Request.done == true)
            {
                bRequest = true;

                ReadbackData = Request.GetData<Color32>();
                Debug.Log(ReadbackData[0]);
            }
        }
    }

    public class FPagePayload
    {
        private static int2 s_InvalidTileIndex = new int2(-1, -1);

        public int2 TileIndex = s_InvalidTileIndex;

        public int ActiveFrame;

        public FPageRequest PageRequest;

        public bool IsReady { get { return (TileIndex.x != s_InvalidTileIndex.x && TileIndex.y != s_InvalidTileIndex.y); } }

        public void ResetTileIndex()
        {
            TileIndex = s_InvalidTileIndex;
        }
    }

    public struct FDrawPageInfo
    {
        public Rect rect;
        public int mip;
        public float2 drawPos;
    }

    public class FPageCell
    {
        public RectInt Rect { get; set; }

        public FPagePayload PagePayload { get; set; }

        public int MipLevel { get; }

        public FPageCell(int x, int y, int width, int height, int mip)
        {
            Rect = new RectInt(x, y, width, height);
            MipLevel = mip;
            PagePayload = new FPagePayload();
        }
    }

    public class FPageTable
    {
        public FPageCell[,] PageCell { get; set; }

        public int MipLevel { get; }

        public int2 pageOffset;
        public int NodeCellCount;
        public int PerCellSize;

        public FPageTable(int mip, int tableSize)
        {
            MipLevel = mip;
            pageOffset = int2.zero;
            PerCellSize = (int)Mathf.Pow(2, mip);
            NodeCellCount = tableSize / PerCellSize;
            PageCell = new FPageCell[NodeCellCount, NodeCellCount];
            for (int i = 0; i < NodeCellCount; i++)
            {
                for (int j = 0; j < NodeCellCount; j++)
                {
                    PageCell[i, j] = new FPageCell(i * PerCellSize, j * PerCellSize, PerCellSize, PerCellSize, MipLevel);
                }
            }
        }

        public void ChangeViewRect(int2 offset, Action<int2> InvalidatePage)
        {
            if (Mathf.Abs(offset.x) >= NodeCellCount || Mathf.Abs(offset.y) > NodeCellCount ||
                offset.x % PerCellSize != 0 || offset.y % PerCellSize != 0)
            {
                for (int i = 0; i < NodeCellCount; i++)
                    for (int j = 0; j < NodeCellCount; j++)
                    {
                        var transXY = GetTransXY(i, j);
                        PageCell[transXY.x, transXY.y].PagePayload.PageRequest = null;
                        InvalidatePage(PageCell[transXY.x, transXY.y].PagePayload.TileIndex);
                    }
                pageOffset = int2.zero;
                return;
            }
            offset.x /= PerCellSize;
            offset.y /= PerCellSize;
            #region clip map
            if (offset.x > 0)
            {
                for (int i = 0; i < offset.x; i++)
                {
                    for (int j = 0; j < NodeCellCount; j++)
                    {
                        var transXY = GetTransXY(i, j);
                        PageCell[transXY.x, transXY.y].PagePayload.PageRequest = null;
                        InvalidatePage(PageCell[transXY.x, transXY.y].PagePayload.TileIndex);
                    }
                }
            }
            else if (offset.x < 0)
            {
                for (int i = 1; i <= -offset.x; i++)
                {
                    for (int j = 0; j < NodeCellCount; j++)
                    {
                        var transXY = GetTransXY(NodeCellCount - i, j);
                        PageCell[transXY.x, transXY.y].PagePayload.PageRequest = null;
                        InvalidatePage(PageCell[transXY.x, transXY.y].PagePayload.TileIndex);
                    }
                }
            }
            if (offset.y > 0)
            {
                for (int i = 0; i < offset.y; i++)
                {
                    for (int j = 0; j < NodeCellCount; j++)
                    {
                        var transXY = GetTransXY(j, i);
                        PageCell[transXY.x, transXY.y].PagePayload.PageRequest = null;
                        InvalidatePage(PageCell[transXY.x, transXY.y].PagePayload.TileIndex);
                    }
                }
            }
            else if (offset.y < 0)
            {
                for (int i = 1; i <= -offset.y; i++)
                {
                    for (int j = 0; j < NodeCellCount; j++)
                    {
                        var transXY = GetTransXY(j, NodeCellCount - i);
                        PageCell[transXY.x, transXY.y].PagePayload.PageRequest = null;
                        InvalidatePage(PageCell[transXY.x, transXY.y].PagePayload.TileIndex);
                    }
                }
            }
            #endregion

            pageOffset += offset;
            while (pageOffset.x < 0)
            {
                pageOffset.x += NodeCellCount;
            }
            while (pageOffset.y < 0)
            {
                pageOffset.y += NodeCellCount;
            }
            pageOffset.x %= NodeCellCount;
            pageOffset.y %= NodeCellCount;
        }

        public FPageCell Get(int x, int y)
        {
            x /= PerCellSize;
            y /= PerCellSize;

            x = (x + pageOffset.x) % NodeCellCount;
            y = (y + pageOffset.y) % NodeCellCount;

            return PageCell[x, y];
        }

        public RectInt GetInverRect(RectInt rect)
        {
            return new RectInt(rect.xMin - pageOffset.x,
                                rect.yMin - pageOffset.y,
                                rect.width,
                                rect.height);
        }

        private int2 GetTransXY(int x, int y)
        {
            return new int2((x + pageOffset.x) % NodeCellCount, (y + pageOffset.y) % NodeCellCount);
        }
    }

    public class FPageRequest
    {
        public int PageX { get; }

        public int PageY { get; }

        public int MipLevel { get; }

        public FPageRequest(int x, int y, int mip)
        {
            PageX = x;
            PageY = y;
            MipLevel = mip;
        }
    }

    public class FTileTexturePool
    {
        public class NodeInfo
        {
            public int id = 0;
            public NodeInfo Next { get; set; }
            public NodeInfo Prev { get; set; }
        }

        private NodeInfo[] allNodes;
        private NodeInfo head = null;
        private NodeInfo tail = null;

        public int First { get { return head.id; } }

        public void Init(int count)
        {
            allNodes = new NodeInfo[count];
            for (int i = 0; i < count; i++)
            {
                allNodes[i] = new NodeInfo()
                {
                    id = i,
                };
            }
            for (int i = 0; i < count; i++)
            {
                allNodes[i].Next = (i + 1 < count) ? allNodes[i + 1] : null;
                allNodes[i].Prev = (i != 0) ? allNodes[i - 1] : null;
            }
            head = allNodes[0];
            tail = allNodes[count - 1];
        }

        public bool SetActive(int id)
        {
            if (id < 0 || id >= allNodes.Length)
                return false;

            var node = allNodes[id];
            if (node == tail)
            {
                return true;
            }

            Remove(node);
            AddLast(node);
            return true;
        }

        private void AddLast(NodeInfo node)
        {
            var lastTail = tail;
            lastTail.Next = node;
            tail = node;
            node.Prev = lastTail;
        }

        private void Remove(NodeInfo node)
        {
            if (head == node)
            {
                head = node.Next;
            }
            else
            {
                node.Prev.Next = node.Next;
                node.Next.Prev = node.Prev;
            }
        }
    }

    public class RuntimeVirtualTextureProducer
    {
        public int TableSize;

        public int MaxMipLevel { get { return (int)math.log2(TableSize); } }


        public Mesh DrawPageMesh;

        public Material DrawPageMaterial;

        public MaterialPropertyBlock DrawPageParameter;


        public FPageTable[] PageTable;

        public List<FPageRequest> PageRequest = new List<FPageRequest>();

        public Dictionary<int2, FPageCell> ActivePageMap = new Dictionary<int2, FPageCell>();



        public RuntimeVirtualTextureProducer()
        {

        }

        public FPageRequest Request(int x, int y, int mip)
        {
            // 是否已经在请求队列中
            foreach (var r in PageRequest)
            {
                if (r.PageX == x && r.PageY == y && r.MipLevel == mip)
                    return null;
            }

            // 加入待处理列表
            var request = new FPageRequest(x, y, mip);
            PageRequest.Add(request);

            return request;
        }

        protected void LoadPage(in int x, in int y, FPageCell PageCell)
        {
            if (PageCell == null)
                return;

            // 正在加载中,不需要重复请求
            if (PageCell.PagePayload.PageRequest != null)
                return;

            // 新建加载请求
            PageCell.PagePayload.PageRequest = Request(x, y, PageCell.MipLevel);
        }

        public void RequestPageData(VirtualTextureVolume VTVolume, in int x, in int y, int mip)
        {
            if (mip > MaxMipLevel || mip < 0 || x < 0 || y < 0 || x >= TableSize || y >= TableSize)
                return;

            // 找到当前页表
            FPageCell PageCell = PageTable[mip].Get(x, y);

            if (PageCell == null) { return; }

            if (!PageCell.PagePayload.IsReady)
            {
                LoadPage(x, y, PageCell);

                //向上找到最近的父节点
                while (mip < MaxMipLevel && !PageCell.PagePayload.IsReady)
                {
                    mip++;
                    PageCell = PageTable[mip].Get(x, y);
                }
            }

            if (PageCell.PagePayload.IsReady)
            {
                // 激活对应的平铺贴图块
                VTVolume.VirtualTexture.SetActive(PageCell.PagePayload.TileIndex);
                PageCell.PagePayload.ActiveFrame = Time.frameCount;
                return;
            }

            return;
        }

        public void ProducePageTable(CommandBuffer CmdBuffer, VirtualTextureVolume VTVolume)
        {
            // 将页表数据写入页表贴图
            var currentFrame = (byte)Time.frameCount;
            var DrawPageList = new List<FDrawPageInfo>();

            foreach (var ActivePage in ActivePageMap)
            {
                var PageCell = ActivePage.Value;

                // 只写入当前帧活跃的页表
                if (PageCell.PagePayload.ActiveFrame != Time.frameCount)
                    continue;

                var table = PageTable[PageCell.MipLevel];
                var offset = table.pageOffset;
                var perSize = table.PerCellSize;
                var lb = new int2((PageCell.Rect.xMin - offset.x * perSize), (PageCell.Rect.yMin - offset.y * perSize));
                while (lb.x < 0)
                {
                    lb.x += TableSize;
                }
                while (lb.y < 0)
                {
                    lb.y += TableSize;
                }

                DrawPageList.Add(new FDrawPageInfo()
                {
                    rect = new Rect(lb.x, lb.y, PageCell.Rect.width, PageCell.Rect.height),
                    mip = PageCell.MipLevel,
                    drawPos = new Vector2((float)PageCell.PagePayload.TileIndex.x / 255,
                    (float)PageCell.PagePayload.TileIndex.y / 255),
                });
            }

            DrawPageList.Sort((a, b) =>
            {
                return -(a.mip.CompareTo(b.mip));
            });

            if (DrawPageList.Count == 0)
            {
                return;
            }

            var PageInfoList = new Vector4[DrawPageList.Count];
            var TransfromMatrix = new Matrix4x4[DrawPageList.Count];

            for (int i = 0; i < DrawPageList.Count; i++)
            {
                float size = DrawPageList[i].rect.width / TableSize;
                PageInfoList[i] = new Vector4(DrawPageList[i].drawPos.x, DrawPageList[i].drawPos.y, DrawPageList[i].mip / 255f, 0);
                TransfromMatrix[i] = Matrix4x4.TRS(new Vector3(DrawPageList[i].rect.x / TableSize, DrawPageList[i].rect.y / TableSize), Quaternion.identity, new Vector3(size, size, size));
            }

            CmdBuffer.SetRenderTarget(VTVolume.VirtualTexture.PageTableTexture);

            DrawPageParameter.Clear();
            DrawPageParameter.SetVectorArray("_PageInfoBuffer", PageInfoList);
            DrawPageParameter.SetMatrixArray("_MVP_Matrix", TransfromMatrix);

            CmdBuffer.DrawMeshInstanced(DrawPageMesh, 0, DrawPageMaterial, 0, TransfromMatrix, TransfromMatrix.Length, DrawPageParameter);
        }

        protected void DrawMesh(CommandBuffer CmdBuffer)
        {

        }

        protected void Compression(CommandBuffer CmdBuffer)
        {

        }

        protected void Copy(CommandBuffer CmdBuffer)
        {

        }

        protected void DrawTileToVT(CommandBuffer CmdBuffer, VirtualTextureVolume VTVolume, in int2 PageID, FPageRequest Request)
        {
            if (!VTVolume.VirtualTexture.SetActive(PageID))
                return;

            int x = Request.PageX;
            int y = Request.PageY;
            int perSize = (int)Mathf.Pow(2, Request.MipLevel);
            x = x - x % perSize;
            y = y - y % perSize;

            var PageSize = VTVolume.VirtualTexture.PageSize;
            var Padding = VTVolume.VirtualTexture.TileSizePadding * perSize * (VTVolume.DrawRect.width / PageSize) / VTVolume.VirtualTexture.TileSize;
            Rect VolumeRect = new Rect(VTVolume.DrawRect.xMin + (float)x / PageSize * VTVolume.DrawRect.width - Padding,
                                     VTVolume.DrawRect.yMin + (float)y / PageSize * VTVolume.DrawRect.height - Padding,
                                     VTVolume.DrawRect.width / PageSize * perSize + 2f * Padding,
                                     VTVolume.DrawRect.width / PageSize * perSize + 2f * Padding);


            RectInt DrawRect = new RectInt(PageID.x * VTVolume.VirtualTexture.TileSizePadding, PageID.y * VTVolume.VirtualTexture.TileSizePadding, VTVolume.VirtualTexture.TileSizePadding, VTVolume.VirtualTexture.TileSizePadding);
            
            /*var terRect = Rect.zero;
            foreach (var ter in TerrainList)
            {
                if (!ter.isActiveAndEnabled)
                {
                    continue;
                }
                terRect.xMin = ter.transform.position.x;
                terRect.yMin = ter.transform.position.z;
                terRect.width = ter.terrainData.size.x;
                terRect.height = ter.terrainData.size.z;
                if (!realRect.Overlaps(terRect))
                {
                    continue;
                }
                var needDrawRect = realRect;
                needDrawRect.xMin = Mathf.Max(realRect.xMin, terRect.xMin);
                needDrawRect.yMin = Mathf.Max(realRect.yMin, terRect.yMin);
                needDrawRect.xMax = Mathf.Min(realRect.xMax, terRect.xMax);
                needDrawRect.yMax = Mathf.Min(realRect.yMax, terRect.yMax);
                var scaleFactor = drawPos.width / realRect.width;
                var position = new Rect(drawPos.x + (needDrawRect.xMin - realRect.xMin) * scaleFactor,
                                        drawPos.y + (needDrawRect.yMin - realRect.yMin) * scaleFactor,
                                        needDrawRect.width * scaleFactor,
                                        needDrawRect.height * scaleFactor);
                var scaleOffset = new Vector4(
                                        needDrawRect.width / terRect.width,
                                        needDrawRect.height / terRect.height,
                                        (needDrawRect.xMin - terRect.xMin) / terRect.width,
                                        (needDrawRect.yMin - terRect.yMin) / terRect.height);
                // 构建变换矩阵
                float l = position.x * 2.0f / tileTexSize.x - 1;
                float r = (position.x + position.width) * 2.0f / tileTexSize.x - 1;
                float b = position.y * 2.0f / tileTexSize.y - 1;
                float t = (position.y + position.height) * 2.0f / tileTexSize.y - 1;
                var mat = new Matrix4x4();
                mat.m00 = r - l;
                mat.m03 = l;
                mat.m11 = t - b;
                mat.m13 = b;
                mat.m23 = -1;
                mat.m33 = 1;

                // 绘制贴图
                Graphics.SetRenderTarget(mVTTileBuffer, mDepthBuffer);
                m_DrawTextureMateral.SetMatrix(Shader.PropertyToID("_ImageMVP"), GL.GetGPUProjectionMatrix(mat, true));
                m_DrawTextureMateral.SetVector("_BlendTile", scaleOffset);
                int layerIndex = 0;
                foreach (var alphamap in ter.terrainData.alphamapTextures)
                {
                    m_DrawTextureMateral.SetTexture("_Blend", alphamap);
                    int index = 1;
                    for (; layerIndex < ter.terrainData.terrainLayers.Length && index <= 4; layerIndex++)
                    {
                        var layer = ter.terrainData.terrainLayers[layerIndex];
                        var nowScale = new Vector2(ter.terrainData.size.x / layer.tileSize.x,
                            ter.terrainData.size.z / layer.tileSize.y);
                        var tileOffset = new Vector4(nowScale.x * scaleOffset.x,
                            nowScale.y * scaleOffset.y, scaleOffset.z * nowScale.x, scaleOffset.w * nowScale.y);
                        m_DrawTextureMateral.SetVector($"_TileOffset{index}", tileOffset);
                        m_DrawTextureMateral.SetTexture($"_Diffuse{index}", layer.diffuseTexture);
                        m_DrawTextureMateral.SetTexture($"_Normal{index}", layer.normalMapTexture ?? defaultNormal);
                        index++;
                    }
                    var tempCB = new CommandBuffer();
                    tempCB.DrawMesh(mQuad, Matrix4x4.identity, m_DrawTextureMateral, 0, layerIndex <= 4 ? 0 : 1);
                    Graphics.ExecuteCommandBuffer(tempCB);//DEBUG
                }
            }*/
        }

        public void RenderPage(CommandBuffer CmdBuffer, VirtualTextureVolume VTVolume)
        {
            if (PageRequest.Count <= 0)
                return;

            // 优先处理mipmap等级高的请求
            PageRequest.Sort((x, y) => { return x.MipLevel.CompareTo(y.MipLevel); });

            int ProcessCount = 2;
            while (ProcessCount > 0 && PageRequest.Count > 0)
            {
                ProcessCount--;

                // 将第一个请求从等待队列移到运行队列
                FPageRequest Request = PageRequest[PageRequest.Count - 1];
                PageRequest.RemoveAt(PageRequest.Count - 1);

                // 开始渲染
                // 找到对应页表
                FPageCell PageCell = PageTable[Request.MipLevel].Get(Request.PageX, Request.PageY);
                if (PageCell == null || PageCell.PagePayload.PageRequest != Request) { return; }

                PageCell.PagePayload.PageRequest = null;

                int2 PageID = VTVolume.VirtualTexture.RequestTile();
                DrawTileToVT(CmdBuffer, VTVolume, PageID, Request);

                PageCell.PagePayload.TileIndex = PageID;
                ActivePageMap[PageID] = PageCell;
            }

            using (new ProfilingScope(CmdBuffer, ProfilingSampler.Get(LandscapeSamplerId.RenderVTPage)))
            {
                using (new ProfilingScope(CmdBuffer, ProfilingSampler.Get(LandscapeSamplerId.DrawVTMesh)))
                {
                    DrawMesh(CmdBuffer);
                }

                using (new ProfilingScope(CmdBuffer, ProfilingSampler.Get(LandscapeSamplerId.CompressionVT)))
                {
                    Compression(CmdBuffer);
                }

                using (new ProfilingScope(CmdBuffer, ProfilingSampler.Get(LandscapeSamplerId.CopyVT)))
                {
                    Copy(CmdBuffer);
                }
            }
        }
    }

    public class VirtualTextureSystem
    {
        protected VirtualTextureVolume RPVTVolume;
        protected RuntimeVirtualTextureProducer RPVTProducer;

        public VirtualTextureSystem()
        {

        }

        public void Initialize()
        {
            RPVTVolume = LandscapeManager.VTVolumeProxy;
            RPVTProducer = new RuntimeVirtualTextureProducer();
        }

        public void Update(bool bReadReady, NativeArray<Color32> FeedbackData, CommandBuffer CmdBuffer)
        {
            if (bReadReady)
            {
                // 激活对应页表
                foreach (Color32 Feedback in FeedbackData)
                {
                    RPVTProducer.RequestPageData(RPVTVolume, Feedback.r, Feedback.g, Feedback.b);
                }

                // PageTable
                RPVTProducer.ProducePageTable(CmdBuffer, RPVTVolume);
            }

            RPVTProducer.RenderPage(CmdBuffer, RPVTVolume);
        }

        public void Release()
        {

        }
    }
}
