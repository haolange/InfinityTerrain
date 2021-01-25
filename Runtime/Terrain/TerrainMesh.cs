using System;
using UnityEngine;
using System.Collections.Generic;

namespace Landscape.Terrain
{
    [Serializable]
    public struct TerrainVertexData
    {
        public string name;
        public int[] IndexArray { get; set; }
        public Vector2[] UVArray { get; set; }
        public Vector3[] VertexArray { get; set; }
    }

    public class TerrainMesh
    {
        public static Mesh BuildSectionMesh(bool FlipEdge, int NumQuad, float GridSize = 1)
        {
            List<int> IndexBuffer = new List<int>();
            List<Vector3> VertexBuffer = new List<Vector3>();
            List<Vector2> TextureCoord0 = new List<Vector2>();

            //////////////////////////////////////////////////////////////////////////////////
            // VertexBuffer
            for (int j = 0; j < NumQuad + 1; j++)
            {
                float z = Mathf.Lerp(-0.5f, 0.5f, j / (float)NumQuad);

                for (int i = 0; i < NumQuad + 1; i++)
                {
                    float x = Mathf.Lerp(-0.5f, 0.5f, i / (float)NumQuad);

                    TextureCoord0.Add(new Vector2(i / (float)NumQuad, j / (float)NumQuad));
                    //VertexBuffer.Add( new Vector3(x, 0, z) * GridSize);
                    VertexBuffer.Add( new Vector3(x, 0, z) * GridSize + new Vector3( (GridSize / 2), 0, (GridSize / 2) ) );
                }
            }

            //////////////////////////////////////////////////////////////////////////////////
            // IndexBuffer
            for (int j = 0; j < NumQuad; j++)
            {
                for (int i = 0; i < NumQuad; i++)
                {
                    bool flipEdge = false;
                    if (i % 2 == 1) flipEdge = !flipEdge;
                    if (j % 2 == 1) flipEdge = !flipEdge;
                    if (!FlipEdge) flipEdge = false;

                    int i0 = i + j * (NumQuad + 1);
                    int i1 = i0 + 1;
                    int i2 = i0 + (NumQuad + 1);
                    int i3 = i2 + 1;

                    if (!flipEdge)
                    {
                        // tri 1
                        IndexBuffer.Add(i3);
                        IndexBuffer.Add(i1);
                        IndexBuffer.Add(i0);

                        // tri 2
                        IndexBuffer.Add(i0);
                        IndexBuffer.Add(i2);
                        IndexBuffer.Add(i3);
                    }
                    else
                    {
                        // tri 1
                        IndexBuffer.Add(i3);
                        IndexBuffer.Add(i1);
                        IndexBuffer.Add(i2);

                        // tri 2
                        IndexBuffer.Add(i0);
                        IndexBuffer.Add(i2);
                        IndexBuffer.Add(i1);
                    }
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // create mesh
            Mesh mesh = new Mesh();
            if (VertexBuffer != null && VertexBuffer.Count > 0)
            {
                int[] IndexArray = new int[IndexBuffer.Count];
                IndexBuffer.CopyTo(IndexArray);

                Vector3[] VertexArray = new Vector3[VertexBuffer.Count];
                VertexBuffer.CopyTo(VertexArray);

                Vector2[] TextureCoordArray = new Vector2[TextureCoord0.Count];
                TextureCoord0.CopyTo(TextureCoordArray);
                //Debug.Log("VB : " + VertexBuffer.Count);
                //Debug.Log("UV : " + TextureCoord0.Count);
                mesh.vertices = VertexArray;
                mesh.triangles = IndexArray;
                mesh.uv = TextureCoordArray;

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
            return mesh;
        }

        public static TerrainVertexData BuildSectionVertexData(bool FlipEdge, int NumQuad, float GridSize = 1)
        {
            List<int> IndexBuffer = new List<int>();
            List<Vector3> VertexBuffer = new List<Vector3>();
            List<Vector2> TextureCoord0 = new List<Vector2>();

            //////////////////////////////////////////////////////////////////////////////////
            // VertexBuffer
            for (int j = 0; j < NumQuad + 1; j++)
            {
                float z = Mathf.Lerp(-0.5f, 0.5f, j / (float)NumQuad);

                for (int i = 0; i < NumQuad + 1; i++)
                {
                    float x = Mathf.Lerp(-0.5f, 0.5f, i / (float)NumQuad);

                    TextureCoord0.Add(new Vector2(i / (float)NumQuad, j / (float)NumQuad));
                    //VertexBuffer.Add( new Vector3(x, 0, z) * GridSize);
                    VertexBuffer.Add( new Vector3(x, 0, z) * GridSize + new Vector3( (GridSize / 2), 0, (GridSize / 2) ) );
                }
            }

            //////////////////////////////////////////////////////////////////////////////////
            // IndexBuffer
            for (int j = 0; j < NumQuad; j++)
            {
                for (int i = 0; i < NumQuad; i++)
                {
                    bool flipEdge = false;
                    if (i % 2 == 1) flipEdge = !flipEdge;
                    if (j % 2 == 1) flipEdge = !flipEdge;
                    if (!FlipEdge) flipEdge = false;

                    int i0 = i + j * (NumQuad + 1);
                    int i1 = i0 + 1;
                    int i2 = i0 + (NumQuad + 1);
                    int i3 = i2 + 1;

                    if (!flipEdge)
                    {
                        // tri 1
                        IndexBuffer.Add(i3);
                        IndexBuffer.Add(i1);
                        IndexBuffer.Add(i0);

                        // tri 2
                        IndexBuffer.Add(i0);
                        IndexBuffer.Add(i2);
                        IndexBuffer.Add(i3);
                    }
                    else
                    {
                        // tri 1
                        IndexBuffer.Add(i3);
                        IndexBuffer.Add(i1);
                        IndexBuffer.Add(i2);

                        // tri 2
                        IndexBuffer.Add(i0);
                        IndexBuffer.Add(i2);
                        IndexBuffer.Add(i1);
                    }
                }
            }


            //////////////////////////////////////////////////////////////////////////////////
            // create mesh
            TerrainVertexData TerrainVF = new TerrainVertexData();
            if (VertexBuffer != null && VertexBuffer.Count > 0)
            {
                int[] IndexArray = new int[IndexBuffer.Count];
                IndexBuffer.CopyTo(IndexArray);

                Vector3[] VertexArray = new Vector3[VertexBuffer.Count];
                VertexBuffer.CopyTo(VertexArray);

                Vector2[] UVArray = new Vector2[TextureCoord0.Count];
                TextureCoord0.CopyTo(UVArray);

                TerrainVF.VertexArray = VertexArray;
                TerrainVF.IndexArray = IndexArray;
                TerrainVF.UVArray = UVArray;
            }
            return TerrainVF;
        }
    }
}
