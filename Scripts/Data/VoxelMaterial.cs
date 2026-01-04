using UnityEngine;

namespace VoxelDestructionPro.Data
{
    [CreateAssetMenu(menuName = "Voxel Destruction/Voxel Material", fileName = "VoxelMaterial")]
    public class VoxelMaterial : ScriptableObject
    {
        [Tooltip("Unique identifier for this voxel material.")]
        public string materialId = System.Guid.NewGuid().ToString();

        [Tooltip("Human readable label for editors and debugging.")]
        public string displayName = "New Material";
    }
}
