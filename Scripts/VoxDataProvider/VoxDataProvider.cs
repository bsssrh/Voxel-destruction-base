using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.VoxDataProviders
{
    [RequireComponent(typeof(VoxelObjBase))]
    public class VoxDataProvider : MonoBehaviour
    {
        protected VoxelObjBase targetObj;

        private void Start()
        {
            targetObj = GetComponent<VoxelObjBase>();
            Load(false);
        }

        public virtual void Load(bool editorMode)
        {
            targetObj = GetComponent<VoxelObjBase>();
            if (editorMode)
                targetObj.Clear();
        }

        public virtual void Clear()
        {
            targetObj = GetComponent<VoxelObjBase>();
            targetObj.Clear();
        }

        public virtual void ResetVoxelObject()
        {
            Clear();
            Load(false);
        }
    }
}
