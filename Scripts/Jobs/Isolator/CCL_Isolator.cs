using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Interfaces;
using VoxelDestructionPro.Settings;

namespace VoxelDestructionPro.Jobs.Isolator
{
     public class CCL_Isolator : VoxelJob, IVoxIsolation
     {
         private CCL_Job cclJob;
         private JobHandle handle;
     
         private NativeArray<Voxel> voxelNative;
         private NativeArray<ushort> labelingNative;
         
         public CCL_Isolator(IsoSettings.IsolationOrigin originType, VoxelData data)
         {
             dataDisposed = false;
     
             voxelNative = new NativeArray<Voxel>(data.voxels.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
             labelingNative = new NativeArray<ushort>(data.voxels.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
             
             cclJob = new CCL_Job()
             {
                 length = data.length,
                 inputVoxels = voxelNative,
                 labeling = labelingNative,
                 originType = originType
             };
         }
         
         public void Begin(VoxelData data)
         {
             Profiler.BeginSample("Starting Isolator");
     
             if (dataDisposed)
                 return;
             
             data.voxels.CopyTo(voxelNative);
             
             handle = cclJob.Schedule();
             Profiler.EndSample();
         }
     
         public bool IsFinished()
         {
             return handle.IsCompleted;
         }
     
         public NativeArray<ushort> GetResults()
         {
             handle.Complete();
     
             return cclJob.labeling;
         }
     
         protected override void DisposeAll()
         {
             handle.Complete();
     
             voxelNative.Dispose();
             labelingNative.Dispose();
         }
     }
}