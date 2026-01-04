using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Interfaces;
using VoxelDestructionPro.Settings;

namespace VoxelDestructionPro.Jobs.Destruction
{
    public class VoxelDestructor : VoxelJob, IDestructor
    {
        private SphereDestructorJob sphereJob;
        private CubeDestructorJob cubeJob;
        private LineDestructorJob lineJob;
        
        private JobHandle handle;
        private int length;

        private NativeList<int> outputIndex;
    
        public VoxelDestructor(int3 length)
        {
            this.length = length.x * length.y * length.z;
            
            outputIndex = new NativeList<int>(Allocator.Persistent);
            sphereJob = new SphereDestructorJob()
            {
                indexLength = length.xy,
                outputIndex = outputIndex,
            };
            cubeJob = new CubeDestructorJob()
            {
                indexLength = length.xy,
                outputIndex = outputIndex,
            };
            lineJob = new LineDestructorJob()
            {
                indexLength = length.xy,
                outputIndex = outputIndex
            };
        }
    
        public void Prepare(DestructionData data)
        {        
            outputIndex.Clear();
            
            if (data.destructionType == DestructionData.DestructionType.Sphere)
            {
                sphereJob.getRadiusSqr = data.range * data.range;
                sphereJob.targetPoint = data.start;
            
                handle = sphereJob.Schedule(length, default);
            }
            else if (data.destructionType == DestructionData.DestructionType.Cube)
            {
                cubeJob.cubeHalfExtend = data.range;
                cubeJob.targetPoint = data.start;
            
                handle = cubeJob.Schedule(length, default);
            }
            else if (data.destructionType == DestructionData.DestructionType.Line)
            {
                lineJob.radiusSqr = data.range * data.range;
                lineJob.startPoint = data.start;
                lineJob.endPoint = data.end;

                handle = lineJob.Schedule(length, default);
            }
        }

        public NativeList<int> GetData()
        {
            handle.Complete();
        
            return outputIndex;
        }

        public bool isFinished()
        {
            return handle.IsCompleted;
        }

        protected override void DisposeAll()
        {
            handle.Complete();
            outputIndex.Dispose();
        }
    }
}