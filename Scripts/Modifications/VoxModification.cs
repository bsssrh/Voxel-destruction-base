using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.VoxelModifications
{
    [RequireComponent(typeof(VoxelObjBase))]
    public class VoxModification : MonoBehaviour
    {
        protected VoxelObjBase targetObj;

        protected DynamicVoxelObj dyn_targetObj => targetObj as DynamicVoxelObj;
        
        protected virtual void Awake()
        {
            targetObj = GetComponent<VoxelObjBase>();
        }
    }
}