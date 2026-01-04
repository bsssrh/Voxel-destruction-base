using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using VoxelDestructionPro.Data;
using Random = Unity.Mathematics.Random;

namespace VoxelDestructionPro.Jobs.Fragmenter
{
    /// <summary>
    /// Splits the removed voxels into different fragments using a sphere shape check
    /// </summary>
    [BurstCompile]
    public struct SphereFragmenterJob : IJob
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

        public uint seed;
        public int sphereMin;
        public int sphereMax;
        public float fragmentCover;
        
        public int3 indexLength;
        
        public void Execute()
        {
            //We always clear lists before using since they get reused
            outputFragments.Clear();
            outputLengths.Clear();

            for (int i = removedVoxels.Length - 1; i >= 0; i--)
                if (voxels[removedVoxels[i]].active == 0)
                    removedVoxels.RemoveAt(i);
            
            if (removedVoxels.Length == 0)
                return;
            
            Random r = new Random(seed);
            Voxel emptyVoxel = Voxel.emptyVoxel;
            
            //Estimates the fragment count based on the cover and average sphere radius
            int frag = (int)((removedVoxels.Length / ((sphereMin + sphereMax) / 2f)) * fragmentCover);
            
            while (removedVoxels.Length > 0 && frag > 0)
            {
                frag--;
                int3 startPos = To3D(removedVoxels[r.NextInt(0, removedVoxels.Length)]);
                
                int sphereRadius = r.NextInt(sphereMin, sphereMax);
                
                outputLengths.Add(startPos);

                byte resultMask = 0;
                
                int3 axis;
                for (axis.z = 0; axis.z <= sphereRadius; axis.z++)
                {
                    resultMask = 1;
                    
                    for (axis.y = 0; axis.y <= sphereRadius; axis.y++)
                    {
                        for (axis.x = 0; axis.x <= sphereRadius; axis.x++)
                        {
                            float distance = math.lengthsq(axis);

                            if (distance > sphereRadius)
                            {
                                outputFragments.Add(emptyVoxel);
                                continue;
                            }

                            int3 global = axis + startPos;
                            
                            if (global.x >= indexLength.x || 
                                global.y >= indexLength.y ||
                                global.z >= indexLength.z)
                            {
                                outputFragments.Add(emptyVoxel);
                                continue;
                            }
                        
                            int globalIndex = To1D(global);
                        
                            for (int i = 0; i < removedVoxels.Length; i++)
                            {
                                if (removedVoxels[i] == globalIndex)
                                {
                                    removedVoxels.RemoveAt(i);
                                    outputFragments.Add(voxels[globalIndex]);

                                    if (voxels[globalIndex].active > 0)
                                        resultMask = 0;
                                    
                                    globalIndex = -1;
                                    break;
                                }
                            }
                        
                            if (globalIndex != -1)
                                outputFragments.Add(emptyVoxel);
                        }
                    }

                    if (resultMask == 1)
                        break;
                }
                
                outputLengths.Add(new int3(sphereRadius, sphereRadius, axis.z) + 1 + startPos);
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