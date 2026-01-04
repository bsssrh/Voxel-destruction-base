using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelDestructionPro.Data.Fragmenter;

namespace VoxelDestructionPro.Settings
{
    [CreateAssetMenu(fileName = "Dynamic Settings", menuName = "VoxelDestruction/DynSettings")]
    public class DynSettings : ScriptableObject
    {
        public enum DestructionMode
        {
            Remove, SingleFragment, SphereBasedFragments, VoxelFragment
        }

        [Header("General")] 
        
        [Tooltip("The destruction mode: \nRemove: Will remove the destroyed voxel from the mesh\n" +
                 "SingleFragment: Will create a single fragment from the removed voxels. Not recommended\n" +
                 "Sphere Based Fragments: Creates fragments by checking if voxels fall into a sphere radius\n" +
                 "Voxel Fragment: Spawns a certain amount of single voxels (voxelPrefab field)")]
        public DestructionMode destructionMode = DestructionMode.SphereBasedFragments;

        [Header("Prefabs")] 
        
        [Tooltip("Used for SingleFragment and Sphere based fragments, should have a Voxel object attached")]
        public GameObject fragmentPrefab;
        [Tooltip("Used for VoxelFragments")]
        public GameObject voxelPrefab;

        [Header("Default settings")] 
        
        [Tooltip("These are the default settings for the SphereBasedFragments destruction mode, " +
                 "if custom settings get attached to the AddDestruction call this will not be used")]
        public SphereFragmenterData defaultSphereSettings;
        [Tooltip("These are the default settings for the VoxelFragment destruction mode, " +
                 "if custom settings get attached to the AddDestruction call this will not be used")]
        public VoxelFragmenterData defaultVoxelSettings;

        [Header("Performance")] 
        
        [Tooltip("Defines how many fragments are allowed to be spawned per frame")]
        public int maxFragmentsPerFrame = 20;
    }
}