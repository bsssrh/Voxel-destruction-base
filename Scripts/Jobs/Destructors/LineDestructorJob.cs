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
    /// Finds all voxels that fall into a defined line shape
    /// </summary>
    [BurstCompile]
    public struct LineDestructorJob : IJobFor
    {
        public NativeList<int> outputIndex;

        public float3 startPoint;
        public float3 endPoint;
        public float radiusSqr;

        public int2 indexLength;

        public void Execute(int index)
        {
            float3 point = To3D(index);
            if (IsPointWithinRadius(point, startPoint, endPoint, radiusSqr))
            {
                outputIndex.Add(index);
            }
        }

        private float3 To3D(int index)
        {
            int z = index / (indexLength.x * indexLength.y);
            int idx = index - (z * indexLength.x * indexLength.y);
            int y = idx / indexLength.x;
            int x = idx % indexLength.x;
            return new float3(x, y, z);
        }

        private bool IsPointWithinRadius(float3 point, float3 start, float3 end, float radiusSqr)
        {
            float3 lineVec = end - start;
            float3 pointVec = point - start;
            float t = math.dot(pointVec, lineVec) / math.dot(lineVec, lineVec);

            t = math.clamp(t, 0f, 1f);

            float3 closestPoint = start + t * lineVec;
            float3 diff = point - closestPoint;

            return math.lengthsq(diff) <= radiusSqr;
        }
    }
}