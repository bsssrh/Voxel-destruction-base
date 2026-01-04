using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Settings
{
    [CreateAssetMenu(fileName = "MeshSettings", menuName = "VoxelDestruction/MeshSettings")]
    public class MeshSettingsObj : ScriptableObject
    {
        public enum MeshCalculationType
        {
            Simple, Parallel3
        }
        
        public enum EmptyAction
        {
            Destroy, Deactive, None
        }

        [Header("General")] 
        
        [Tooltip("Defines what should happen once the Voxel object mesh is empty")]
        public EmptyAction emptyAction = EmptyAction.Destroy;
        
        [Header("Collision")] 
        
        [Tooltip("Colliders need to be baked in order to work with the physics engine. This moves this baking process into a job, which is strongly recommended to avoid lag")]
        public bool useThreadedCollisionBaking = true;
        [Tooltip("The collider cooking options, only get used if useThreadedCollisionBaking is on. It is recommended to leave this at none for faster baking times, but you can also aim for faster physic simulation")]
        public MeshColliderCookingOptions cookingOptions = MeshColliderCookingOptions.None;
        [Tooltip("When using threaded collisionbaking on first baking the mesh collider the object temporary has no collision mesh. This option allows you to freeze a possible rigidbody while baking")]
        public bool freezeRbWhileBaking = true;
    
        [Header("Performance")]

        [Tooltip("Parallel3 is faster, while Simple allocates less memory. Use simple with small voxel objects")]
        public MeshCalculationType mesherType = MeshCalculationType.Simple;
        [Tooltip("The mesh will get constructed inside a job, adds additional delay, but improves performance for complex meshes")]
        public bool meshConstructionJob = true;
    }
}