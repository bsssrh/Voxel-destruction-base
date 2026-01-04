using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelDestructionPro.Data
{
    public class CachedVoxelData
    {
        public Voxel[] voxels;
        public Color[] palette;
        public int3 length;

        public CachedVoxelData(Voxel[] voxels, Color[] palette, int3 length)
        {
            this.voxels = voxels;
            this.palette = palette;
            this.length = length;
        }

        public CachedVoxelData GetCopy()
        {
            Voxel[] v = voxels.Select(t => t.CreateCopy()).ToArray();
            Color[] c = palette.Select(t => new Color(t.r, t.g, t.b, t.a)).ToArray();

            return new CachedVoxelData(v, c, length);
        }
    }
}