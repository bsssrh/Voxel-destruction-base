using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Fragmenter
{
    [BurstCompile]
    public struct SingleFragmenterJob : IJob
    {
        public NativeList<int> removedVoxels;
        [ReadOnly]
        public NativeArray<Voxel> voxels;
        
        public NativeList<Voxel> outputFragments;
        /// <summary>
        /// 2 inputs for each VoxelFragment
        /// 1 = bounding box min in Voxeldata 3D space
        /// 2 = bounding box max in Voxeldata 3D space
        /// </summary>
        public NativeList<int3> outputLengths;
        
        public int2 indexLength;
        
        public void Execute()
        {
            outputFragments.Clear();
            outputLengths.Clear();

            for (int i = removedVoxels.Length - 1; i >= 0; i--)
                if (voxels[removedVoxels[i]].active == 0)
                    removedVoxels.RemoveAt(i);
            
            if (removedVoxels.Length == 0)
                return;
            
            int3 min = To3D(removedVoxels[0]);
            int3 max = min;

            for (int i = 1; i < removedVoxels.Length; i++)
            {
                int3 global = To3D(removedVoxels[i]);
                
                if (global.x < min.x)
                    min.x = global.x;
                if (global.y < min.y)
                    min.y = global.y;
                if (global.z < min.z)
                    min.z = global.z;
                            
                if (global.x > max.x)
                    max.x = global.x;
                if (global.y > max.y)
                    max.y = global.y;
                if (global.z > max.z)
                    max.z = global.z;
            }
            
            int3 length = max - min;

            outputLengths.Add(min);
            outputLengths.Add(max + new int3(1));
            
            Voxel emptyVoxel = Voxel.emptyVoxel;
            int3 axis;
            for (axis.z = 0; axis.z <= length.z; axis.z++)
            {
                for (axis.y = 0; axis.y <= length.y; axis.y++)
                {
                    for (axis.x = 0; axis.x <= length.x; axis.x++)
                    {
                        int globalIndex = To1D(axis + min);

                        for (int i = 0; i < removedVoxels.Length; i++)
                        {
                            if (removedVoxels[i] == globalIndex)
                            {
                                removedVoxels.RemoveAt(i);
                                outputFragments.Add(voxels[globalIndex]);
                                globalIndex = -1;
                                break;
                            }
                        }
                        
                        if (globalIndex != -1)
                            outputFragments.Add(emptyVoxel);
                    }
                }
            }
        }
        
        private int3 To3D(int index)
        {
            int z = index / (indexLength.x * indexLength.y);
            int idx = index - (z * indexLength.x * indexLength.y);
            int y = idx / indexLength.x;
            int x = idx % indexLength.x;
            return new int3(x, y, z);
        }
        
        private int To1D(int3 pos)
        {
            return pos.x + indexLength.x * (pos.y + indexLength.y * pos.z);
        }
    }
}