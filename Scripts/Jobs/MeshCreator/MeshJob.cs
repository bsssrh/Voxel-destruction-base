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
    public struct MeshJob : IJob
    {
        [ReadOnly]
        public NativeList<float3> vert;
        [ReadOnly]
        public NativeList<int> tris;
        [ReadOnly]
        public NativeList<Color> colors;
        [ReadOnly]
        public NativeList<float3> normals;
        
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
            
            meshData.SetVertexBufferParams(vert.Length, vertexAttributes);
            
            var vertBuffer = meshData.GetVertexData<float3>(0);
            for (int i = 0; i < vert.Length; i++)
                vertBuffer[i] = vert[i];
            
            var normBuffer = meshData.GetVertexData<float3>(1);
            for (int i = 0; i < normals.Length; i++)
                normBuffer[i] = normals[i];

            var colorBuffer = meshData.GetVertexData<Color>(2);
            for (int i = 0; i < colors.Length; i++)
                colorBuffer[i] = colors[i];

            if (vert.Length < 65000)
            {
                meshData.SetIndexBufferParams(tris.Length, IndexFormat.UInt16);
                var indexBuffer = meshData.GetIndexData<short>();
                for (int i = 0; i < tris.Length; i++)
                    indexBuffer[i] = (short)tris[i];
            }
            else
            {
                meshData.SetIndexBufferParams(tris.Length, IndexFormat.UInt32);
                var indexBuffer = meshData.GetIndexData<int>();
                for (int i = 0; i < tris.Length; i++)
                    indexBuffer[i] = tris[i];
            }
            
            meshData.subMeshCount = 1;
            var subMeshDescriptor = new SubMeshDescriptor(0, tris.Length)
            {
                topology = MeshTopology.Triangles,
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

            for (int i = 0; i < vert.Length; i++)
            {
                if (vert[i].x > max.x)
                    max.x = vert[i].x;
                if (vert[i].y > max.y)
                    max.y = vert[i].y;
                if (vert[i].z > max.z)
                    max.z = vert[i].z;
                
                if (vert[i].x < min.x)
                    min.x = vert[i].x;
                if (vert[i].y < min.y)
                    min.y = vert[i].y;
                if (vert[i].z < min.z)
                    min.z = vert[i].z;
            }

            float3 rawMax = max - min;
            boundsValues[0] = (rawMax / 2f) + min;
            boundsValues[1] = rawMax;
        }
    }   
}