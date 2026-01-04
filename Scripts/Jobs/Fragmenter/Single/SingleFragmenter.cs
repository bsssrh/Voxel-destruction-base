using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Interfaces;

namespace VoxelDestructionPro.Jobs.Fragmenter
{
    /// <summary>
    /// Creates a single fragment from all the Voxels that got removed
    /// </summary>
    public class SingleFragmenter : VoxelJob, IFragmenter
    {
        private SingleFragmenterJob fragmenterJob;
        private JobHandle handle;

        private NativeList<int> removedVoxels;
        private NativeArray<Voxel> voxels;
        
        private NativeList<Voxel> outputFragments;
        private NativeList<int3> outputLengths;
        
        public SingleFragmenter(VoxelData data)
        {
            removedVoxels = new NativeList<int>(Allocator.Persistent);
            voxels = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent);
            outputFragments = new NativeList<Voxel>(Allocator.Persistent);
            outputLengths = new NativeList<int3>(Allocator.Persistent);
            
            fragmenterJob = new SingleFragmenterJob()
            {
                removedVoxels = removedVoxels,
                voxels = voxels,
                outputFragments = outputFragments,
                outputLengths = outputLengths,
                indexLength = data.length.xy,
            };
        }
        
        public void StartFragmenting(VoxelData data, NativeList<int> _removedVoxels, object fragmentData = null)
        {
            data.voxels.CopyTo(voxels);
            removedVoxels.CopyFrom(_removedVoxels);

            handle = fragmenterJob.Schedule();
        }

        public bool IsFinished()
        {
            return handle.IsCompleted;
        }

        public VoxelData[] CreateFragments(VoxelData data, out Vector3[] positions)
        {
            handle.Complete();
            
            positions = null;
            int fragments = outputLengths.Length / 2;
            if (fragments == 0 || fragments > 1)
                return null;

            VoxelData[] vData = new VoxelData[1];
            positions = new Vector3[1];

            int3 boundsMin = outputLengths[0];
            int3 boundsMax = outputLengths[1];
            
            positions[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
            int3 length = boundsMax - boundsMin;
            
            int totalLength = length.x * length.y * length.z;
            NativeArray<Voxel> fragmentVoxels = new NativeArray<Voxel>(totalLength, Allocator.Persistent);
            fragmentVoxels.CopyFrom(outputFragments.AsArray());
            
            vData[0] = new VoxelData(fragmentVoxels, data.palette, length);

            return vData;
        }

        public bool UseVoxelFragments()
        {
            return false;
        }

        protected override void DisposeAll()
        {
            handle.Complete();

            removedVoxels.Dispose();
            voxels.Dispose();

            outputLengths.Dispose();
            outputFragments.Dispose();
        }
    }
}