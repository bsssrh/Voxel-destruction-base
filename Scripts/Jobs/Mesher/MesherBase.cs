using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using Debug = UnityEngine.Debug;

namespace VoxelDestructionPro.Jobs.Mesher
{
    public class MesherBase : VoxelJob
    {
        protected bool isEmpty;
    
        public void Prepare(VoxelData data)
        {
            isEmpty = math.all(data.length == int3.zero);

            if (isEmpty)
                return;
        
            PrepareMesher(data);
        }

        protected virtual void PrepareMesher(VoxelData data)
        {
        
        }

        public Mesh CompleteMeshBuilding()
        {
            if (isEmpty)
                return null;

            return GetMeshFromJob();
        }
    
        protected int GetMaskLength(int3 length)
        {
            int max = Mathf.Max(length.x, length.y, length.z);

            return max * max;
        }

        protected virtual Mesh GetMeshFromJob()
        {
            Debug.LogError("Get Mesh method not implemented!");
            throw new NotImplementedException();
        }
    }
   
}