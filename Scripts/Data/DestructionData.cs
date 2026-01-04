using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Data
{
    public class DestructionData
    {
        /// <summary>
        /// For sphere and cube destruction this is the target point
        /// </summary>
        public Vector3 start;
        /// <summary>
        /// For line destruction, start-end
        /// </summary>
        public Vector3 end;
        /// <summary>
        /// The range from the target point of line
        /// </summary>
        public float range;
        /// <summary>
        /// There are different destruction types
        /// </summary>
        public DestructionType destructionType;
    
        public DestructionData(DestructionType destructionType, Vector3 start, Vector3 end, float range)
        {
            this.start = start;
            this.end = end;
            this.range = range;
            this.destructionType = destructionType;
        }
    
        public enum DestructionType
        {
            Sphere, Line, Cube
        }

        [System.Obsolete("Use DestructionType instead.")]
        public enum DestructionShapeType
        {
            Sphere, Line, Cube
        }

        public bool IsValidData()
        {
            return range >= 1f;
        }

        [System.Obsolete("Use DestructionType instead.")]
        public DestructionData(DestructionShapeType destructionType, Vector3 start, Vector3 end, float range)
            : this((DestructionType)destructionType, start, end, range)
        {
        }
    }
}
