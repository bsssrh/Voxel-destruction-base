using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Data
{
    /// <summary>
    /// Serializable helper that lets designers choose whether all materials are affected
    /// or limit interactions to specific voxel material types.
    /// </summary>
    [System.Serializable]
    public class VoxelMaterialFilter
    {
        [Tooltip("When enabled all voxel materials can be affected. Disable to restrict to specific types.")]
        public bool affectAllMaterials = true;

        [Tooltip("Materials that can be affected when 'Affect All Materials' is disabled.")]
        public List<VoxelMaterialType> materialTypes = new();

        public IEnumerable<VoxelMaterialType> GetFilter() => affectAllMaterials ? null : materialTypes;
    }
}
