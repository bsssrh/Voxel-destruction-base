using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using Random = Unity.Mathematics.Random;

namespace VoxelDestructionPro.Jobs.Fragmenter
{
    /// <summary>
    /// Returns isolated single voxels
    /// </summary>
    [BurstCompile]
    public struct VoxelFragmenterJob : IJob
    {
        public NativeList<int> removedVoxels;
        [ReadOnly]
        public NativeArray<Voxel> voxels;
        
        public NativeList<int3> outputVoxels;

        public uint seed;
        //Range 0-1 amount of voxels that should get created
        public float cover;
        public int2 indexLength;
        
        public void Execute()
        {
            outputVoxels.Clear();
            
            for (int i = removedVoxels.Length - 1; i >= 0; i--)
                if (voxels[removedVoxels[i]].active == 0)
                    removedVoxels.RemoveAt(i);
            
            if (removedVoxels.Length == 0)
                return;
            
            Random r = new Random(seed);
            cover = math.clamp(cover, 0f, 1f);

            int voxelCount = (int)math.round(removedVoxels.Length * cover);

            for (int i = 0; i < voxelCount; i++)
            {
                int vIndex = r.NextInt(0, removedVoxels.Length);
                
                outputVoxels.Add(To3D(removedVoxels[vIndex]));
                removedVoxels.RemoveAt(vIndex);
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
    }
}