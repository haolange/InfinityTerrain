using UnityEngine;
using Unity.Mathematics;
using Landscape.Terrain;

namespace Landscape.Utils
{
    public static class LandscapeUtility 
    {
        public static float4[] LODColor = new float4[7] { new float4(1, 1, 1, 1), new float4(1, 0, 0, 1), new float4(0, 1, 0, 1), new float4(0, 0, 1, 1), new float4(1, 1, 0, 1), new float4(1, 0, 1, 1), new float4(0, 1, 1, 1) };
        
        public static float Squared(float A)
        {
            return A * A;
        }

        public static float DistSquared(Vector3 V1, Vector3 V2)
        {
            return Squared(V2.x - V1.x) + Squared(V2.y - V1.y) + Squared(V2.z - V1.z);
        }

        public static float LogX(float Base, float Value)
        {
            return Mathf.Log(Value) / Mathf.Log(Base);
        }

        public static float GetBoundRadius(Bounds BoundBox)
        {
            Vector3 Extents = BoundBox.extents;
	        return Mathf.Max( Mathf.Max(Mathf.Abs(Extents.x), Mathf.Abs(Extents.y)), Mathf.Abs(Extents.z) );
        }

        public static float4x4 GetProjectionMatrix(float HalfFOV, float Width, float Height, float MinZ, float MaxZ)
        {
            float4 column0 = new float4(1.0f / Mathf.Tan(HalfFOV),	0.0f,									0.0f,							                        0.0f);
            float4 column1 = new float4(0.0f,						Width / Mathf.Tan(HalfFOV) / Height,	    0.0f,							                        0.0f);
            float4 column2 = new float4(0.0f,						    0.0f,									MinZ == MaxZ ? 1.0f : MaxZ / (MaxZ - MinZ),			    1.0f);
            float4 column3 = new float4(0.0f,						    0.0f,								   -MinZ * (MinZ == MaxZ ? 1.0f : MaxZ / (MaxZ - MinZ)),	0.0f);

            return new float4x4(column0, column1, column2, column3);
        }

        public static float ComputeBoundsScreenRadiusSquared(float SphereRadius, Vector3 BoundsOrigin, Vector3 ViewOrigin, Matrix4x4 ProjMatrix)
        {
            float DistSqr = DistSquared(BoundsOrigin, ViewOrigin) * ProjMatrix.m23;

            float ScreenMultiple = Mathf.Max(0.5f * ProjMatrix.m00, 0.5f * ProjMatrix.m11);
            ScreenMultiple *= SphereRadius;

            return (ScreenMultiple * ScreenMultiple) / Mathf.Max(1, DistSqr);
        }

        public static float ComputeBoundsScreenRadiusSquared(float SphereRadius, Vector3 BoundsOrigin, Vector3 ViewOrigin, float4x4 ProjMatrix)
        {
            float DistSqr = DistSquared(BoundsOrigin, ViewOrigin) * ProjMatrix.c2.z;

            float ScreenMultiple = Mathf.Max(0.5f * ProjMatrix.c0.x, 0.5f * ProjMatrix.c1.y);
            ScreenMultiple *= SphereRadius;

            return (ScreenMultiple * ScreenMultiple) / Mathf.Max(1, DistSqr);
        }

        public static bool IntersectAABBFrustum(Plane[] plane, Bounds bound)
        {
            for (int i = 0; i < 6; i++)
            {
                float3 normal = plane[i].normal;
                float distance = plane[i].distance;

                float dist = math.dot(normal, bound.center) + distance;
                float radius = math.dot(bound.extents, math.abs(normal));

                if (dist + radius < 0) {
                    return false;
                }
            }

            return true;
        }

        public static int GetLODFromScreenSize(SectionLODData LODSettings, float InScreenSizeSquared, float InViewLODScale, out float OutFractionalLOD)
        {
            float ScreenSizeSquared = InScreenSizeSquared / InViewLODScale;
            
            if (ScreenSizeSquared <= LODSettings.LastLODScreenSizeSquared) {
                OutFractionalLOD = LODSettings.LastLODIndex;
                return LODSettings.LastLODIndex;
            } else if (ScreenSizeSquared > LODSettings.LOD1ScreenSizeSquared) {
                OutFractionalLOD = (LODSettings.LOD0ScreenSizeSquared - Mathf.Min(ScreenSizeSquared, LODSettings.LOD0ScreenSizeSquared)) / (LODSettings.LOD0ScreenSizeSquared - LODSettings.LOD1ScreenSizeSquared);
                return 0;
            } else {
                OutFractionalLOD = 1 + LogX(LODSettings.LODOnePlusDistributionScalarSquared, LODSettings.LOD1ScreenSizeSquared / ScreenSizeSquared);
                return (int)OutFractionalLOD;
            }
        }

        public static void GetNeighborSection(int SectorSize, TerrainSection[] TerrainSections)
        {
            int[,] Direction = new int[,] { { 0, 1 }, { 0, -1 }, { -1, 0 }, { 1, 0 } };

            // 遍历块
            for (int i = 0; i < SectorSize * SectorSize; ++i)
            {
                TerrainSection Section = TerrainSections[i];

                int x = i % SectorSize;
                int y = i / SectorSize;

                // 遍历方向
                for (int j = 0; j < 4; ++j)
                {
                    int sx = x + Direction[j, 0];
                    int sy = y + Direction[j, 1];

                    // 判断边界
                    if (sx >= 0 && sx < SectorSize && sy >= 0 && sy < SectorSize)
                    {
                        Section.NeighborSection[j] = TerrainSections[sy * SectorSize + sx];
                    }
                    else
                    {
                        Section.NeighborSection[j] = null;
                    }
                }
            }
        }

        public static int GetSectionNumFromTerrainSize(int InTerrainSize)
        {
            int SectionNum = 0;

            switch (InTerrainSize)
            {
                case 1024:
                    {
                        SectionNum = 16;
                        break;
                    }

                case 512:
                    {
                        SectionNum = 8;
                        break;
                    }

                case 256:
                    {
                        SectionNum = 4;
                        break;
                    }

                case 128:
                    {
                        SectionNum = 2;
                        break;
                    }
            }

            return SectionNum;
        }

        public static void DrawBound(Bounds b)
		{
			// bottom
			var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
			var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
			var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
			var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

			Debug.DrawLine(p1, p2, Color.blue);
			Debug.DrawLine(p2, p3, Color.red);
			Debug.DrawLine(p3, p4, Color.yellow);
			Debug.DrawLine(p4, p1, Color.magenta);

			// top
			var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
			var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
			var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
			var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

			Debug.DrawLine(p5, p6, Color.blue);
			Debug.DrawLine(p6, p7, Color.red);
			Debug.DrawLine(p7, p8, Color.yellow);
			Debug.DrawLine(p8, p5, Color.magenta);

			// sides
			Debug.DrawLine(p1, p5, Color.white);
			Debug.DrawLine(p2, p6, Color.gray);
			Debug.DrawLine(p3, p7, Color.green);
			Debug.DrawLine(p4, p8, Color.cyan);
		}

		public static void DrawBound(Bounds b, Color DebugColor)
		{
			// bottom
			var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
			var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
			var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
			var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

			Debug.DrawLine(p1, p2, DebugColor);
			Debug.DrawLine(p2, p3, DebugColor);
			Debug.DrawLine(p3, p4, DebugColor);
			Debug.DrawLine(p4, p1, DebugColor);

			// top
			var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
			var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
			var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
			var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

			Debug.DrawLine(p5, p6, DebugColor);
			Debug.DrawLine(p6, p7, DebugColor);
			Debug.DrawLine(p7, p8, DebugColor);
			Debug.DrawLine(p8, p5, DebugColor);

			// sides
			Debug.DrawLine(p1, p5, DebugColor);
			Debug.DrawLine(p2, p6, DebugColor);
			Debug.DrawLine(p3, p7, DebugColor);
			Debug.DrawLine(p4, p8, DebugColor);
		}

        public static void DrawRect(Rect rect, Color color)
        {

            Vector3[] line = new Vector3[5];

            line[0] = new Vector3(rect.x,rect.y,0);

            line[1] = new Vector3(rect.x+rect.width, rect.y, 0);

            line[2] = new Vector3(rect.x + rect.width, rect.y + rect.height, 0);

            line[3] = new Vector3(rect.x, rect.y + rect.height, 0);

            line[4] = new Vector3(rect.x, rect.y, 0);

            for(int i = 0; i < line.Length - 1; i++)
            {
                Debug.DrawLine(line[i], line[i + 1], color);
            }
        }

        public static void DrawPlane(Vector3 position, Vector3 normal)
        {

            Vector3 v3;

            if (normal.normalized != Vector3.forward)
                v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
            else
                v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

            var corner0 = position + v3;
            var corner2 = position - v3;
            var q = Quaternion.AngleAxis(90.0f, normal);
            v3 = q * v3;
            var corner1 = position + v3;
            var corner3 = position - v3;

            Debug.DrawLine(corner0, corner2, Color.green);
            Debug.DrawLine(corner1, corner3, Color.green);
            Debug.DrawLine(corner0, corner1, Color.green);
            Debug.DrawLine(corner1, corner2, Color.green);
            Debug.DrawLine(corner2, corner3, Color.green);
            Debug.DrawLine(corner3, corner0, Color.green);
            Debug.DrawRay(position, normal, Color.red);
        }
    }
}
