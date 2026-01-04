using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Interfaces
{
    /// <summary>
    /// This is used to find isolated pieces inside voxel objects
    /// </summary>
    public interface IVoxIsolation : IDisposable
    {
        public void Begin(VoxelData data);
        public bool IsFinished();
        public NativeArray<ushort> GetResults();
    }
}