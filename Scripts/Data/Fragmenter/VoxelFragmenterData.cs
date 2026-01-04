using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Data.Fragmenter
{
    [Serializable]
    public class VoxelFragmenterData
    {
        [Range(0.0f, 1.0f)]
        public float cover = 0.5f;
    }
}