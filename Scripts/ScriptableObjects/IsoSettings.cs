using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelDestructionPro.Settings
{
    [CreateAssetMenu(fileName = "IsolationSettings", menuName = "VoxelDestruction/IsoSettings")]
    public class IsoSettings : ScriptableObject
    {
        public enum IsolationMode
        {
            None, Remove, Fragment
        }

        public enum IsolationOrigin
        {
            XPos, XNeg, YPos, YNeg, ZPos, ZNeg, None
        }
    
        [Header("Isolation")] 
        
        [Tooltip("The isolation mode of this voxel object, None will not use isolation at all, " +
                 "Remove will just remove isolated Voxels from the object and " +
                 "Fragment will create a new voxel object based on the isolationFragmentPrefab with a new Voxeldata")]
        public IsolationMode isolationMode = IsolationMode.Fragment;
        [Tooltip("Defines if the isolator should get run on creation of the object")]
        public bool runIsolationOnStart = true;
    
        [Header("Fragment isolation")]
    
        [Tooltip("The gameobject that gets instantiated as a fragment, if it contains a voxelobject the voxeldata will automatically be assigned")]
        public GameObject isolationFragmentPrefab;

        [Tooltip("The minimum voxel count for a fragment to appear")]
        public int minVoxelCount = 0;
    }   
}
