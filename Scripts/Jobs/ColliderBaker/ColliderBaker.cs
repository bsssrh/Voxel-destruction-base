using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace VoxelDestructionPro.Jobs.ColliderBaker
{
    public class ColliderBaker : VoxelJob
    {
        private ColliderBakeJob bakeJob;
        private JobHandle handle;

        private MeshCollider collider;
        private Mesh mesh;
    
        public ColliderBaker()
        {
            bakeJob = new ColliderBakeJob()
            {
                bakeInfo = new MeshBakeInformation()
            };
        }

        public void BakeCollider(MeshCollider mc, Mesh m)
        {
            if (mc == null || m == null)
                return;
        
            collider = mc;
            mesh = m;
        
            bakeJob.bakeInfo.CookingOptions = mc.cookingOptions;
            bakeJob.bakeInfo.InstanceID = m.GetInstanceID();
            bakeJob.bakeInfo.Convex = (byte)(mc.convex ? 1 : 0);

            handle = bakeJob.Schedule();
        }

        public bool isCompleted() => handle.IsCompleted;

        public void FinishBaking()
        {
            handle.Complete();
        
            if (collider == null)
                return;
            
            collider.sharedMesh = mesh;
        }
    
        protected override void DisposeAll()
        {
            handle.Complete();
        }
    }
}