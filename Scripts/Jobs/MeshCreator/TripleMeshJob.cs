using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelDestructionPro.Jobs.Mesher
{
    [BurstCompile]
    public struct TripleMeshJob : IJob
    {
        [ReadOnly]
        public NativeList<float3> vert1;
        [ReadOnly]
        public NativeList<int> tris1;
        [ReadOnly]
        public NativeList<Color> colors1;
        [ReadOnly]
        public NativeList<float3> normals1;
        [ReadOnly]
        public NativeList<float3> vert2;
        [ReadOnly]
        public NativeList<int> tris2;
        [ReadOnly]
        public NativeList<Color> colors2;
        [ReadOnly]
        public NativeList<float3> normals2;
        [ReadOnly]
        public NativeList<float3> vert3;
        [ReadOnly]
        public NativeList<int> tris3;
        [ReadOnly]
        public NativeList<Color> colors3;
        [ReadOnly]
        public NativeList<float3> normals3;
        
        public Mesh.MeshDataArray dataArray;
        
        //0 = Center
        //1 = Size
        public NativeArray<Vector3> boundsValues;
        
        public void Execute()
        {
            Mesh.MeshData meshData = dataArray[0];
            
            // Set the vertex buffer
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                3,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory
            );
            vertexAttributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0);
            vertexAttributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 1);
            vertexAttributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, stream: 2);

            int vertexCount = vert1.Length + vert2.Length + vert3.Length;
            meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
            
            var vertBuffer = meshData.GetVertexData<float3>(0);
            var normBuffer = meshData.GetVertexData<float3>(1);
            var colorBuffer = meshData.GetVertexData<Color>(2);

            for (int i = 0; i < vert1.Length; i++)
            {
                vertBuffer[i] = vert1[i];
                normBuffer[i] = normals1[i];
                colorBuffer[i] = colors1[i];
            }

            for (int i = 0; i < vert2.Length; i++)
            {
                vertBuffer[vert1.Length + i] = vert2[i];
                normBuffer[vert1.Length + i] = normals2[i];
                colorBuffer[vert1.Length + i] = colors2[i];
            }

            int vertL = vert1.Length + vert2.Length;
            for (int i = 0; i < vert3.Length; i++)
            {
                vertBuffer[vertL + i] = vert3[i];
                normBuffer[vertL + i] = normals3[i];
                colorBuffer[vertL + i] = colors3[i];
            }

            if (vertexCount < 65000)
            {
                meshData.SetIndexBufferParams(tris1.Length + tris2.Length + tris3.Length, IndexFormat.UInt16);
                var indexBuffer = meshData.GetIndexData<short>();
                for (int i = 0; i < tris1.Length; i++)
                    indexBuffer[i] = (short)tris1[i];
                for (int i = 0; i < tris2.Length; i++)
                    indexBuffer[i + tris1.Length] = (short)(tris2[i] + vert1.Length);
                int trisL = tris1.Length + tris2.Length;
                for (int i = 0; i < tris3.Length; i++)
                    indexBuffer[i + trisL] = (short)(tris3[i] + vertL);
            }
            else
            {
                meshData.SetIndexBufferParams(tris1.Length + tris2.Length + tris3.Length, IndexFormat.UInt32);
                var indexBuffer = meshData.GetIndexData<int>();
                for (int i = 0; i < tris1.Length; i++)
                    indexBuffer[i] = tris1[i];
                for (int i = 0; i < tris2.Length; i++)
                    indexBuffer[i + tris1.Length] = tris2[i] + vert1.Length;
                int trisL = tris1.Length + tris2.Length;
                for (int i = 0; i < tris3.Length; i++)
                    indexBuffer[i + trisL] = tris3[i] + vertL;   
            }
            
            meshData.subMeshCount = 1;
            var subMeshDescriptor = new SubMeshDescriptor(0, tris1.Length + tris2.Length + tris3.Length)
            {
                topology = MeshTopology.Triangles
            };
            meshData.SetSubMesh(0, subMeshDescriptor, 
                MeshUpdateFlags.DontRecalculateBounds | 
                MeshUpdateFlags.DontValidateIndices |
                MeshUpdateFlags.DontNotifyMeshUsers | 
                MeshUpdateFlags.DontResetBoneBounds);

            CalculateBounds();
        }
        
        /// <summary>
        /// Recalculate bounds is too slow
        /// </summary>
        private void CalculateBounds()
        {
            float3 max = new float3(float.MinValue);
            float3 min = new float3(float.MaxValue);

            for (int i = 0; i < vert1.Length; i++)
            {
                if (vert1[i].x > max.x)
                    max.x = vert1[i].x;
                if (vert1[i].y > max.y)
                    max.y = vert1[i].y;
                if (vert1[i].z > max.z)
                    max.z = vert1[i].z;
                
                if (vert1[i].x < min.x)
                    min.x = vert1[i].x;
                if (vert1[i].y < min.y)
                    min.y = vert1[i].y;
                if (vert1[i].z < min.z)
                    min.z = vert1[i].z;
            }
            for (int i = 0; i < vert2.Length; i++)
            {
                if (vert2[i].x > max.x)
                    max.x = vert2[i].x;
                if (vert2[i].y > max.y)
                    max.y = vert2[i].y;
                if (vert2[i].z > max.z)
                    max.z = vert2[i].z;
                
                if (vert2[i].x < min.x)
                    min.x = vert2[i].x;
                if (vert2[i].y < min.y)
                    min.y = vert2[i].y;
                if (vert2[i].z < min.z)
                    min.z = vert2[i].z;
            }
            for (int i = 0; i < vert3.Length; i++)
            {
                if (vert3[i].x > max.x)
                    max.x = vert3[i].x;
                if (vert3[i].y > max.y)
                    max.y = vert3[i].y;
                if (vert3[i].z > max.z)
                    max.z = vert3[i].z;
                
                if (vert3[i].x < min.x)
                    min.x = vert3[i].x;
                if (vert3[i].y < min.y)
                    min.y = vert3[i].y;
                if (vert3[i].z < min.z)
                    min.z = vert3[i].z;
            }

            float3 rawMax = max - min;
            boundsValues[0] = (rawMax / 2f) + min;
            boundsValues[1] = rawMax;
        }
    }
}