using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VoxelDestructionPro.Jobs.ColliderBaker
{
    [BurstCompile]
    public struct ColliderBakeJob : IJob
    {
        public MeshBakeInformation bakeInfo;

        public void Execute()
        {
            Physics.BakeMesh(bakeInfo.InstanceID, bakeInfo.Convex == 1, bakeInfo.CookingOptions);
        }
    }

    public struct MeshBakeInformation
    {
        public MeshBakeInformation(int _instanceID, byte _convex, MeshColliderCookingOptions cookingOptions)
        {
            InstanceID = _instanceID;
            Convex = _convex;
            CookingOptions = cookingOptions;
        }
    
        public int InstanceID;
        public byte Convex;
        public MeshColliderCookingOptions CookingOptions;
    }
}