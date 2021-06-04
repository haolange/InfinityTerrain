using UnityEngine;
using Landscape.Utils;
using Unity.Collections;
using Landscape.Terrain;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace Landscape.Rendering
{
    public enum LandscapeSamplerId
    {
        DrawFoliage,
        DrawFeedback,
        DrawTerrain,
        RenderVTPage,
        DrawVTMesh,
        CompressionVT,
        CopyVT,
        Max,
    }

    public static class ShaderParameter_ID
    {
        public static int VTFeedbackParams = Shader.PropertyToID("_VTFeedbackParams");

        public static int LastLOD = Shader.PropertyToID("_LastLOD");
        public static int TerrainSize = Shader.PropertyToID("_TerrainSize");
        public static int SectorSize = Shader.PropertyToID("_SectorSize");
        public static int SectionSize = Shader.PropertyToID("_SectionSize");
        public static int BufferOffset = Shader.PropertyToID("_BufferOffset");
        public static int TerrainBuffer = Shader.PropertyToID("_TerrainBuffer");
        public static int SplatArray = Shader.PropertyToID("_SplatArray");
        public static int HeightArray = Shader.PropertyToID("_HeightArray");
        public static int TangentArray = Shader.PropertyToID("_TangentArray");
        public static int AlbedoArray = Shader.PropertyToID("_AlbedoArray");
        public static int NormalArray = Shader.PropertyToID("_NormalArray");
    }

    public class LandscapeRenderPass : ScriptableRenderPass
    {
        int LastLandscapeCount = 0;
        private int ColorBufferID = Shader.PropertyToID("_CameraColorTexture");
        private int DepthBufferID = Shader.PropertyToID("_CameraDepthTexture");

        private RenderingData m_renderingData;
        private LandscapeRender m_LandscapeRender;
        private ScriptableRenderer m_scriptableRender;

        public LandscapeRenderPass(LandscapeRender InLandscapeRender)
        {
            m_LandscapeRender = InLandscapeRender;
        }

        ~LandscapeRenderPass()
        {

        }

        public void InitPassData(ref RenderingData renderingData, ScriptableRenderer scriptableRender)
        {
            m_renderingData = renderingData;
            m_scriptableRender = scriptableRender;
        }

        public override void Configure(CommandBuffer CmdBuffer, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int SizeX = m_renderingData.cameraData.camera.pixelWidth / 8;
            int SizeY = m_renderingData.cameraData.camera.pixelHeight / 8;
            RenderTextureDescriptor FeedbackBufferDesc = new RenderTextureDescriptor { width = SizeX, height = SizeY, volumeDepth = 1, dimension = TextureDimension.Tex2D, graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm, depthBufferBits = 32, mipCount = -1, useMipMap = false, autoGenerateMips = false, bindMS = false, msaaSamples = 1 };
        }

        public override void Execute(ScriptableRenderContext renderContext, ref RenderingData renderingData)
        {
            if (!LandscapeManager.bStartRender) { return; }

            //Init CommandBuffer
            CommandBuffer CmdBuffer = CommandBufferPool.Get("Render Landscape");
            renderContext.ExecuteCommandBuffer(CmdBuffer);
            CmdBuffer.Clear();

            #region GatherTerrainBatch
            FTerrainBuffer TerrainBuffer = LandscapeManager.TerrainBuffer;
            FTerrainBatchCollector TerrainBatchCollector = LandscapeManager.TerrainBatchCollector;
            TerrainBatchCollector.ResetCollection();

            int SplatOffset = 0;
            bool NeedCopyTexture = LandscapeManager.bCopyTexture || LastLandscapeCount != LandscapeManager.GetLandscapeProxyList().Count;

            for (int SectorIndex = 0; SectorIndex < LandscapeManager.GetLandscapeProxyList().Count; SectorIndex++)
            {
                TerrainComponent landscapeProxy = LandscapeManager.GetLandscapeProxyList()[SectorIndex];

                if (NeedCopyTexture)
                {
                    CmdBuffer.CopyTexture(landscapeProxy.GetHeightTexture(), 0, LandscapeManager.HeightArray, SectorIndex);
                    CmdBuffer.CopyTexture(landscapeProxy.GetNormalTexture(), 0, LandscapeManager.TangentArray, SectorIndex);
                }

                int SplatmapCount = landscapeProxy.UnityTerrainData.alphamapTextureCount;
                if (NeedCopyTexture && SplatmapCount != 0)
                {
                    for (int SplatIndex = 0; SplatIndex < SplatmapCount; SplatIndex++)
                    {
                        CmdBuffer.CopyTexture(landscapeProxy.GetSplatTexture(SplatIndex), 0, LandscapeManager.SplatArray, SplatIndex + SplatOffset);
                    }

                    LandscapeManager.bCopyTexture = false;
                }

                FTerrainBatchInitializer TerrainBatchInitializer;
                TerrainBatchInitializer.HeightmapIndex = SectorIndex;
                TerrainBatchInitializer.SplatmapIndex = SplatOffset;
                TerrainBatchInitializer.SplatmapCount = SplatmapCount;
                TerrainBatchInitializer.TerrainLayer = landscapeProxy.UnityTerrainData.terrainLayers;
                TerrainBatchInitializer.SurfacemapCount = landscapeProxy.UnityTerrainData.alphamapLayers;
                TerrainBatchInitializer.FrustumPlane = GeometryUtility.CalculateFrustumPlanes(renderingData.cameraData.camera);
                TerrainBatchInitializer.MatrixProj = LandscapeUtility.GetProjectionMatrix((renderingData.cameraData.camera.fieldOfView + 30.0f) * 0.5f, renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight, renderingData.cameraData.camera.nearClipPlane, renderingData.cameraData.camera.farClipPlane);
                landscapeProxy.GetTerrainBatch(renderingData.cameraData.camera, TerrainBatchCollector, TerrainBatchInitializer);

                SplatOffset += SplatmapCount;
            }

            LastLandscapeCount = LandscapeManager.GetLandscapeProxyList().Count;
            #endregion //GatherTerrainBatch

            #region RenderLandscape
            Mesh[] Meshes = LandscapeManager.TerrainMeshs;
            Material[] Materials = LandscapeManager.TerrainMaterials;
            NativeArray<FTerrainDrawCommand> TerrainDrawCommandList = new NativeArray<FTerrainDrawCommand>(6, Allocator.Temp);

            #region BuildTerrainDrawCommand
            int BufferOffset = 0;
            for (int MDCIndex = 0; MDCIndex < 6; ++MDCIndex)
            {
                List<FTerrainBatch> TerrainBatch = TerrainBatchCollector.GetTerrainBatch(MDCIndex);

                if (TerrainBatch.Count != 0)
                {
                    TerrainBuffer.SetBufferData(BufferOffset, TerrainBatch.Count, TerrainBatch);
                    TerrainDrawCommandList[MDCIndex] = new FTerrainDrawCommand(TerrainBatch.Count, BufferOffset);
                    BufferOffset += TerrainBatch.Count;
                }
                else
                {
                    TerrainDrawCommandList[MDCIndex] = new FTerrainDrawCommand(0, 0);
                }
            }
            #endregion //BuildTerrainDrawCommand

            #region DrawTerrain
            using (new ProfilingScope(CmdBuffer, ProfilingSampler.Get(LandscapeSamplerId.DrawTerrain)))
            {
                CmdBuffer.SetRenderTarget(ColorBufferID);

                for (int LODIndex = 0; LODIndex < 6; ++LODIndex)
                {
                    int InstanceCount = TerrainDrawCommandList[LODIndex].InstanceCount;
                    int InstanceOffset = TerrainDrawCommandList[LODIndex].BufferOffset;

                    if (InstanceCount != 0)
                    {
                        Materials[LODIndex].SetInt(ShaderParameter_ID.LastLOD, 5);
                        Materials[LODIndex].SetInt(ShaderParameter_ID.SectorSize, 32);
                        Materials[LODIndex].SetInt(ShaderParameter_ID.SectionSize, 32);
                        Materials[LODIndex].SetInt(ShaderParameter_ID.TerrainSize, 1024);
                        Materials[LODIndex].SetInt(ShaderParameter_ID.BufferOffset, InstanceOffset);
                        Materials[LODIndex].SetBuffer(ShaderParameter_ID.TerrainBuffer, TerrainBuffer.GetBuffer());
                        Materials[LODIndex].SetTexture(ShaderParameter_ID.SplatArray, LandscapeManager.SplatArray);
                        Materials[LODIndex].SetTexture(ShaderParameter_ID.HeightArray, LandscapeManager.HeightArray);
                        Materials[LODIndex].SetTexture(ShaderParameter_ID.AlbedoArray, LandscapeManager.AlbedoArray);
                        Materials[LODIndex].SetTexture(ShaderParameter_ID.NormalArray, LandscapeManager.NormalArray);
                        Materials[LODIndex].SetTexture(ShaderParameter_ID.TangentArray, LandscapeManager.TangentArray);
                        CmdBuffer.DrawMeshInstancedProcedural(Meshes[LODIndex], 0, Materials[LODIndex], 1, InstanceCount);
                    }
                }
            }
            #endregion //DrawFeedback

            #endregion //RenderLandscape

            //Execute and Release
            renderContext.ExecuteCommandBuffer(CmdBuffer);
            CommandBufferPool.Release(CmdBuffer);
            TerrainDrawCommandList.Dispose();
        }

        public override void FrameCleanup(CommandBuffer CmdBuffer)
        {

        }
    }

    public class LandscapeRender : ScriptableRendererFeature
    {
        private LandscapeRenderPass m_LandscapeRenderPass;


        LandscapeRender()
        {

        }

        ~LandscapeRender()
        {

        }

        public override void Create()
        {
            //Create MeshRender
            m_LandscapeRenderPass = new LandscapeRenderPass(this);
            m_LandscapeRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer scriptableRender, ref RenderingData renderingData)
        {
            m_LandscapeRenderPass.InitPassData(ref renderingData, scriptableRender);
            scriptableRender.EnqueuePass(m_LandscapeRenderPass);
        }
    }
}


