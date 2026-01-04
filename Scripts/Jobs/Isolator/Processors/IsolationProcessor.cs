using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Jobs.Isolator
{
    public class IsolationProcessor : VoxelJob
    {
        private NativeArray<Voxel> voxels;
        private NativeList<Voxel> outputFragments;
        private NativeList<int3> outputLengths;
        private NativeList<int> checkedLabels;

        private IsolationProcessorJob processorJob;
        private JobHandle handle;
        
        public IsolationProcessor(VoxelData data)
        {
            voxels = new NativeArray<Voxel>(data.voxels, Allocator.Persistent);
            outputFragments = new NativeList<Voxel>(Allocator.Persistent);
            outputLengths = new NativeList<int3>(Allocator.Persistent);
            checkedLabels = new NativeList<int>(Allocator.Persistent);
            
            processorJob = new IsolationProcessorJob()
            {
                voxels = voxels,
                outputFragments = outputFragments,
                outputLengths = outputLengths,
                checkedLabels = checkedLabels,
                indexLength = data.length.xy
            };
        }

        public void ProcessIsolationData(NativeArray<ushort> data, VoxelData sourceData)
        {
            if (voxels.Length != sourceData.voxels.Length)
            {
                voxels.Dispose();
                voxels = new NativeArray<Voxel>(sourceData.voxels.Length, Allocator.Persistent);
                processorJob.voxels = voxels;
                processorJob.indexLength = sourceData.length.xy;
            }

            sourceData.voxels.CopyTo(voxels);
            processorJob.isolationData = data;
            
            handle = processorJob.Schedule();
        }

        public bool ProcessorCompleted() => handle.IsCompleted;

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
            
            for (int i = 0; i < fragments; i++)
            {
                int3 boundsMin = outputLengths[i * 2];
                int3 boundsMax = outputLengths[(i * 2) + 1];

                positions[i] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
                
                int3 length = boundsMax - boundsMin;

                int totalLength = length.x * length.y * length.z;
                NativeSlice<Voxel> sourceSlice = new NativeSlice<Voxel>(outputFragments.AsArray(), voxelStartIndex, totalLength);
                
                vData[i] = new VoxelData(sourceSlice, data.palette, length);
                
                voxelStartIndex += totalLength;
            }

            return vData;
        }

        protected override void DisposeAll()
        {
            handle.Complete();
            
            voxels.Dispose();
            outputFragments.Dispose();
            outputLengths.Dispose();
            checkedLabels.Dispose();
        }
    }   
}
