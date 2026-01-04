using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Simple
{
    /// <summary>
    /// A simple job that counts the active voxels inside a voxeldata,
    /// better performance than just looping over the array without a job
    /// </summary>
    [BurstCompile]
    public struct VoxelCountJob : IJob
    {
        [ReadOnly]
        public NativeArray<Voxel> voxels;

        [WriteOnly]
        public NativeArray<int> output;
    
        public void Execute()
        {
            int c = 0;
        
            for (int i = 0; i < voxels.Length; i++)
                if (voxels[i].active != 0)
                    c++;

            output[0] = c;
        }
    }
}
