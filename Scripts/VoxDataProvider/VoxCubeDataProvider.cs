using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.VoxDataProviders
{
    /// <summary>
    /// A simple example on how to procedually generate voxeldata
    ///
    /// Creates a simple cube from a size and a color
    /// </summary>
    public class VoxCubeDataProvider : VoxDataProvider
    {
        [Tooltip("The size of the cube, xyz are set to the same")]
        public int cubeSize = 30;
        [Tooltip("The color of the cube")]
        public Color cubeColor = Color.gray;

        public override void Load(bool editorMode)
        {
            base.Load(editorMode);

            Voxel[] voxels = new Voxel[cubeSize * cubeSize * cubeSize];

            Voxel v = new Voxel(0, 1);
            for (int i = 0; i < voxels.Length; i++)
                voxels[i] = v;

            Color[] palette = new[] { cubeColor };

            VoxelData voxelData = new VoxelData(voxels, palette, new int3(cubeSize));
            
            targetObj.AssignVoxelData(voxelData, editorMode);
        }
    }
}