using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Data.Fragmenter;
using VoxelDestructionPro.Interfaces;
using Random = UnityEngine.Random;

namespace VoxelDestructionPro.Jobs.Fragmenter
{
    /// <summary>
    /// Returns single isolated voxels
    /// </summary>
    public class VoxelFragmenter : VoxelJob, IFragmenter
    {
        private VoxelFragmenterJob fragmenterJob;
        private JobHandle handle;

        private NativeList<int> removedVoxels;
        private NativeArray<Voxel> voxels;
        
        private NativeList<int3> outputFragments;
        
        public VoxelFragmenter(VoxelData data)
        {           
            removedVoxels = new NativeList<int>(Allocator.Persistent);
            voxels = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent);
            outputFragments = new NativeList<int3>(Allocator.Persistent);

            fragmenterJob = new VoxelFragmenterJob()
            {
                indexLength = data.length.xy,
                outputVoxels = outputFragments,
                removedVoxels = removedVoxels,
                voxels = voxels
            };
        }
        
        public void StartFragmenting(VoxelData data, NativeList<int> _removedVoxels, object fragmentData = null)
        {
            if (fragmentData is not VoxelFragmenterData voxelFragData)
                throw new InvalidOperationException();

            fragmenterJob.cover = voxelFragData.cover;
            
            fragmenterJob.seed = (uint)(UnityEngine.Random.value * 123456789);
            removedVoxels.CopyFrom(_removedVoxels);
            data.voxels.CopyTo(voxels);
            
            handle = fragmenterJob.Schedule();
        }

        public bool IsFinished()
        {
            return handle.IsCompleted;
        }

        public VoxelData[] CreateFragments(VoxelData data, out Vector3[] positions)
        {
            handle.Complete();

            positions = new Vector3[outputFragments.Length];

            for (int i = 0; i < outputFragments.Length; i++)
                positions[i] = Int3ToVec3(outputFragments[i]);
            
            return null;
        }
        
        private Vector3 Int3ToVec3(int3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
        
        public bool UseVoxelFragments()
        {
            return true;
        }

        protected override void DisposeAll()
        {
            handle.Complete();
            
            removedVoxels.Dispose();
            voxels.Dispose();
            outputFragments.Dispose();
        }
    }
}
