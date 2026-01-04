using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelDestructionPro.Jobs.Destruction
{
    /// <summary>
    /// Finds all voxels that fall into a defined sphere shape
    /// </summary>
    [BurstCompile]
    public struct SphereDestructorJob : IJobFor
    {
        public NativeList<int> outputIndex;

        public float3 targetPoint;
        public float getRadiusSqr;

        public int2 indexLength;
    
        public void Execute(int index)
        {
            float distance = math.lengthsq(To3D(index) - targetPoint);
        
            if (distance <= getRadiusSqr)
                outputIndex.Add(index);
        }
    
        private float3 To3D(int index)
        {
            int z = index / (indexLength.x * indexLength.y);
            int idx = index - (z * indexLength.x * indexLength.y);
            int y = idx / indexLength.x;
            int x = idx % indexLength.x;
            return new float3(x, y, z);
        }
    }
}