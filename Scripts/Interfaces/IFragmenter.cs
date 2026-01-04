using System;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;

namespace VoxelDestructionPro.Interfaces
{
    /// <summary>
    /// An interface for the Fragmenter that creates the fragment once a destruction occurs
    /// </summary>
    public interface IFragmenter : IDisposable
    {
        /// <summary>
        /// Starts the Fragmenter, wants the voxels that got removed
        /// </summary>
        /// <param name="data"></param>
        /// <param name="_removedVoxels"></param>
        /// <param name="fragmentData"></param>
        public void StartFragmenting(VoxelData data, NativeList<int> _removedVoxels, object fragmentData = null);
        /// <summary>
        /// Is the job finished?
        /// </summary>
        /// <returns></returns>
        public bool IsFinished();
        /// <summary>
        /// Creates the fragments Voxeldata from the job,
        /// positions tells you where to place each fragment
        /// </summary>
        /// <param name="data"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        public VoxelData[] CreateFragments(VoxelData data, out Vector3[] positions);
        /// <summary>
        /// Tells the voxelobject if the fragmenter returns voxels positions instead of voxeldatas,
        /// return true if you want to have single voxel fragments
        /// </summary>
        /// <returns></returns>
        public bool UseVoxelFragments();
    }
}
