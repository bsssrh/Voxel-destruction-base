using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Simple
{
    [BurstCompile]
    public struct VoxelOverride : IJob
    {
        public NativeArray<Voxel> voxels;
        [ReadOnly]
        public NativeArray<ushort> data;
    
        public void Execute()
        {
            Voxel emptyVoxel = Voxel.emptyVoxel;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] <= 1)
                    continue;

                voxels[i] = emptyVoxel;
            }
        }
    }
}
