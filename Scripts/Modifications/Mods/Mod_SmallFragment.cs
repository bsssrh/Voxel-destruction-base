using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Data.Args;
using VoxelDestructionPro.VoxelModifications;
using VoxelDestructionPro.VoxelObjects;
using Random = UnityEngine.Random;

namespace VoxelDestructionPro.VoxelModifications
{
    /// <summary>
    /// A modification that allows better control over small voxel fragments
    /// </summary>
    public class Mod_SmallFragment : VoxModification
    {
        [Tooltip("Once the voxel count is lower than this threshold the fragment is considered small")]
        public int thresholdVoxelCount = 150;

        [Tooltip("Percentage value of how likely it is that the fragment will totally destroy on destruction, 0=0% 1=100%")]
        [Range(0.0f, 1.0f)]
        public float destructionPercentage = 0.5f;
        [Tooltip("If the Fragment is not destroyed a force gets added instead, here you can specify how strong this force should be")]
        public float destructionForce = 10;
        
        private bool isSmallFragment;
    
        private void Start()
        {
            if (targetObj is not DynamicVoxelObj dynVoxObj)
                return;
            
            isSmallFragment = targetObj.ActiveVoxelCount < thresholdVoxelCount;
            if (isSmallFragment)
                dynVoxObj.onVoxelDestruction += OnVoxelDestruction;
        }

        private void OnVoxelDestruction(object sender, VoxDestructionEventArgs e)
        {
            e.BlockDestruction = true;

            if (UnityEngine.Random.value <= destructionPercentage)
                targetObj.objectDestructionRequested = true; //Directly destroy small fragments instead of running a destructor
            else if (TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = (transform.position - e.DestructionDate.start).normalized;

                rb.AddForce(dir * destructionForce, ForceMode.VelocityChange);
            }
        }
    }
}
