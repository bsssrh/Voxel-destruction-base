using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Interfaces
{
    public interface IDestructor : IDisposable
    {
        public void Prepare(DestructionData data);
        public NativeList<int> GetData();
        public bool isFinished();
    }
}