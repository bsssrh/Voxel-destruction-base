using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Mesher
{
    /// <summary>
    /// Takes in a voxeldata restructured in nativelist and calculates the mesh components from it
    /// </summary>
    [BurstCompile]
    public struct GreedyMesherJob : IJob
    {
        public int vertIndex;
        
        //Input voxeldata
        [ReadOnly]
        public NativeArray<Voxel> inputVoxels;
        [ReadOnly] 
        public NativeArray<Color> inputPalette;
        public int3 inputSize;
        public float inputTargetSize;
        
        //Output lists
        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> triangles;
        [WriteOnly] 
        public NativeList<float3> normals;
        [WriteOnly]
        public NativeList<Color> colors;

        public NativeArray<Voxel> mask;

        /*
         These allow you to specify if all calculation should be run
         D defines the direction 0, 1, 2 are the 3 different axis xyz.
         We can schedule 3 different jobs with different startD and endD
         values to run them in parallel.
         We can also just set startD to 0 and endD to 3 and 1 job will run all
         3 axis
         
         Why not use IJobFor and run in parallel?
         NativeLists do not support parallel running, I tried using NativeArrays with
         fixed max index instead, but there are still issues with the parallel job. It 
         caused some weird triangles and wrong indexes
        */
        public int startD;
        public int endD;
        
        public void Execute()
        {
            vertices.Clear();
            triangles.Clear();
            normals.Clear();
            colors.Clear();
            
            Voxel emptyBlock = new Voxel();
                
            for (int d = startD; d < endD; d++)
            {
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;
                var x = new int3();
                var q = new int3();
                
                q[d] = 1;
                
                for (x[d] = -1; x[d] < inputSize[d];)
                {
                    var n = 0;
                    for (x[v] = 0; x[v] < inputSize[v]; ++x[v])
                    {
                        for (x[u] = 0; x[u] < inputSize[u]; ++x[u])
                        {
                            var blockCurrent = (x[d] >= 0) ? GetBlock(x) : emptyBlock;
                            var blockCompare = (x[d] < inputSize[d] - 1) ? GetBlock(x + q) : emptyBlock;

                            if ((blockCurrent.active == blockCompare.active) 
                                || (blockCurrent.active > 0 && blockCompare.active > 0))
                            {
                                mask[n++] = emptyBlock;
                            }
                            else if (blockCurrent.active == 0)
                            {
                                blockCurrent.normal = 1;
                                blockCurrent.color = blockCompare.color;
                                mask[n++] = blockCurrent;
                            }
                            else
                            {
                                blockCompare.normal = 2;
                                blockCompare.color = blockCurrent.color;
                                mask[n++] = blockCompare;
                            }
                        }
                    }

                    x[d]++;
                    
                    n = 0;

                    for (int j = 0; j < inputSize[v]; j++)
                    {
                        for (int i = 0; i < inputSize[u];)
                        {
                            if (mask[n].active > 0 || mask[n].normal != 0)
                            {
                                int w = 1;
                                int h = 1;
                                for (; i + w < inputSize[u] && mask[n + w].Equals(mask[n]); w++) { }
                                
                                var done = false;
                                for (; j + h < inputSize[v]; h++)
                                {
                                    for (int k = 0; k < w; ++k)
                                    {
                                        if (mask[n + k + h * inputSize[u]].active > 0 || !mask[n + k + h * inputSize[u]].Equals(mask[n]))
                                        {
                                            done = true;
                                            break;
                                        }
                                    }
                                    if (done)
                                        break;
                                }

                                x[u] = i;
                                x[v] = j;
                                
                                var du = new int3();
                                du[u] = w;

                                var dv = new int3();
                                dv[v] = h;

                                var blockMinVertex = new float3(-0.5f, -0.5f, -0.5f);

                                AddToMesh(
                                    blockMinVertex + x,
                                    blockMinVertex + x + du,
                                    blockMinVertex + x + du + dv,
                                    blockMinVertex + x + dv,
                                    mask[n],
                                    new Vector3(q.x, q.y, q.z)
                                );

                                 for (int l = 0; l < h; ++l)
                                    for (int k = 0; k < w; ++k)
                                        mask[n + k + l * inputSize[u]] = emptyBlock;

                                i += w;
                                n += w;
                            }
                            else
                            {
                                i++;
                                n++;
                            }
                        }
                    }
                }
            }
        }
        
        private Voxel GetBlock(int3 pos)
        {
            return inputVoxels[pos.x + inputSize.x * (pos.y + inputSize.y * pos.z)];
        }
        
        private void AddToMesh(
            float3 bottomLeft,
            float3 topLeft,
            float3 topRight,
            float3 bottomRight,
            Voxel voxel,
            Vector3 axisMask
        )
        {
            if (inputPalette.Length == 0)
                return;

            int normal = voxel.normal;
            if (normal == 2)
                normal = -1;
            
            triangles.Add(vertIndex);
            triangles.Add(vertIndex + 2 - normal);
            triangles.Add(vertIndex + 2 + normal);
            triangles.Add(vertIndex + 3);
            triangles.Add(vertIndex + 1 + normal);
            triangles.Add(vertIndex + 1 - normal);
            
            vertices.Add(bottomLeft * inputTargetSize);
            vertices.Add(bottomRight * inputTargetSize);
            vertices.Add(topLeft * inputTargetSize);
            vertices.Add(topRight * inputTargetSize);
            vertIndex += 4;
                
            int paletteIndex = math.clamp(voxel.color, 0, inputPalette.Length - 1);
            Color paletteColor = inputPalette[paletteIndex];
            colors.Add(paletteColor);
            colors.Add(paletteColor);
            colors.Add(paletteColor);
            colors.Add(paletteColor);

            Vector3 normalDir = axisMask * -normal;
            normals.Add(normalDir);
            normals.Add(normalDir);
            normals.Add(normalDir);
            normals.Add(normalDir);
        }
    }
}
