using System;
using UnityEngine;

namespace VoxelDestructionPro.Data.Fragmenter
{
    [Serializable]
    public class SphereFragmenterData
    {
        public int minSphereRadius = 5;
        public int maxSphereRadius = 10;
        [Range(0.0f, 1.0f)]
        public float fragmentCover = 0.5f;
    }
}
