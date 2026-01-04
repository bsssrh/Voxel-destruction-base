using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelModifications;

namespace VoxelDestructionPro.VoxelModifications
{
    /// <summary>
    /// Sets a voxelobjects rigidbody mass relative to the active voxel count
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Mod_VoxelMass : VoxModification
    {
        [Tooltip("The mass a single voxel has")]
        public float voxelMass = 0.05f;

        private Rigidbody rb;

        protected override void Awake()
        {
            base.Awake();

            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            targetObj.onVoxeldataChanged += OnVoxeldataChanged;
            
            if (targetObj.CurrentVoxelData != null)
                OnVoxeldataChanged(targetObj.CurrentVoxelData);
        }

        private void OnVoxeldataChanged(VoxelData obj)
        {
            int count = obj.GetActiveVoxelCount();

            rb.mass = count * voxelMass;
        }
    }
}