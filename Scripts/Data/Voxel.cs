using System;

namespace VoxelDestructionPro.Data
{
    public struct Voxel : IEquatable<Voxel>
    {
        public readonly static Voxel emptyVoxel = new Voxel(0, 0);
        
        public Voxel(byte color, byte active)
        {
            this.color = color;
            this.active = active;
            this.normal = 0;
        }
        
        //The color index in palette
        public byte color;
        /// <summary>
        ///A byte is used to mark a voxel as active, since bool is not blitable
        ///This allows allows us to save meta data, to make some voxels harder to
        ///destroy than others
        ///
        /// Anything greater 0 is active
        /// </summary>
        public byte active;
        /// <summary>
        /// The greedy mesher needs this
        /// </summary>
        public byte normal;
        
        public bool Equals(Voxel other)
        {
            return color == other.color && ((active == other.active) || (active > 0 && other.active > 0)) && normal == other.normal;
        }

        public Voxel CreateCopy()
        {
            return new Voxel(color, active);
        }
    }
}