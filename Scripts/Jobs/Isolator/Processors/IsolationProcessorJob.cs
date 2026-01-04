using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Isolator
{
    /// <summary>
    /// This job takes the int data returned by the isolation job
    /// and splits it into the resulting VoxelDatas
    /// </summary>
    [BurstCompile]
    public struct IsolationProcessorJob : IJob
    {
        [ReadOnly]
        public NativeArray<ushort> isolationData;
        [ReadOnly]
        public NativeArray<Voxel> voxels;

        public NativeList<Voxel> outputFragments;
        /// <summary>
        /// 2 inputs for each VoxelFragment
        /// 1 = bounding box min in Voxeldata 3D space
        /// 2 = bounding box max in Voxeldata 3D space
        /// </summary>
        public NativeList<int3> outputLengths;
        public NativeList<int> checkedLabels;

        public int2 indexLength;
        
        public void Execute()
        {
            outputFragments.Clear();
            outputLengths.Clear();
            checkedLabels.Clear();

            Voxel emptyVoxel = Voxel.emptyVoxel;
            
            for (int i = 0; i < isolationData.Length; i++)
            {
                if (isolationData[i] > 1)
                {
                    ushort searchLabel = isolationData[i];
                    
                    for (int j = 0; j < checkedLabels.Length; j++)
                        if (searchLabel == checkedLabels[j])
                        {
                            searchLabel = 0;
                            break;
                        }

                    if (searchLabel == 0)
                        continue;
                    
                    //Not checked label
                    checkedLabels.Add(isolationData[i]);
                    
                    //Identify bounding box
                    int3 labelGlobal = To3D(i);
                    int3 min = labelGlobal;
                    int3 max = labelGlobal;
                    
                    for (int j = 0; j < isolationData.Length; j++)
                    {
                        if (isolationData[j] == searchLabel)
                        {
                            int3 global = To3D(j);

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
                    }
                    
                    int3 length = max - min;

                    outputLengths.Add(min);
                    outputLengths.Add(max + new int3(1));

                    int3 axis;
                    for (axis.z = 0; axis.z <= length.z; axis.z++)
                    {
                        for (axis.y = 0; axis.y <= length.y; axis.y++)
                        {
                            for (axis.x = 0; axis.x <= length.x; axis.x++)
                            {
                                int globalIndex = To1D(axis + min);

                                if (isolationData[globalIndex] == searchLabel)
                                {
                                    //Active voxel
                                    outputFragments.Add(voxels[globalIndex]);
                                }
                                else
                                {
                                    //Inactive voxel
                                    outputFragments.Add(emptyVoxel);
                                }
                            }
                        }
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