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
    /// Fragments removed Voxels using a sphere shape
    /// </summary>
    public class SphereFragmenter : VoxelJob, IFragmenter
    {
        private SphereFragmenterJob fragmenterJob;
        private JobHandle handle;
        
        private NativeList<int> removedVoxels;
        private NativeArray<Voxel> voxels;
        
        private NativeList<Voxel> outputFragments;
        private NativeList<int3> outputLengths;
        
        public SphereFragmenter(VoxelData data)
        {
            removedVoxels = new NativeList<int>(Allocator.Persistent);
            voxels = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent);
            outputFragments = new NativeList<Voxel>(Allocator.Persistent);
            outputLengths = new NativeList<int3>(Allocator.Persistent);
            
            fragmenterJob = new SphereFragmenterJob()
            {
                removedVoxels = removedVoxels,
                voxels = voxels,
                outputFragments = outputFragments,
                outputLengths = outputLengths,
                indexLength = data.length,
            };
        }
    
        public void StartFragmenting(VoxelData data, NativeList<int> _removedVoxels, object fragmentData = null)
        {
            if (fragmentData is not SphereFragmenterData sphereFragData)
                throw new InvalidOperationException();

            fragmenterJob.sphereMax = sphereFragData.maxSphereRadius;
            fragmenterJob.sphereMin = sphereFragData.minSphereRadius;
            fragmenterJob.fragmentCover = sphereFragData.fragmentCover;
            
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

            positions = null;
            int fragments = outputLengths.Length / 2;
            if (fragments == 0)
                return null;
                
            VoxelData[] vData = new VoxelData[fragments];
            positions = new Vector3[fragments];
            
            int voxelStartIndex = 0;

            NativeArray<Voxel> output = outputFragments.AsArray();
            
            for (int i = 0; i < fragments; i++)
            {
                int3 boundsMin = outputLengths[i * 2];
                int3 boundsMax = outputLengths[(i * 2) + 1];

                positions[i] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
                
                int3 length = boundsMax - boundsMin;
                
                //Copy from a nativeslice
                int totalLength = length.x * length.y * length.z;
                NativeSlice<Voxel> sourceSlice = new NativeSlice<Voxel>(output, voxelStartIndex, totalLength);

                vData[i] = new VoxelData(sourceSlice, data.palette, length);
                
                voxelStartIndex += totalLength;
            }

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
