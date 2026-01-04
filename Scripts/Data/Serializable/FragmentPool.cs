using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Data.Serializable
{
    [Serializable]
    public class PooledFragments
    {
        [Tooltip("The prefab that should be pooled")]
        public GameObject prefab;
        [Tooltip("How many instances should always be available")]
        public int instanceCount;
    }
}