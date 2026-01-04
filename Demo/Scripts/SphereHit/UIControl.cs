using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Better.StreamingAssets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.Tools;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo 
{
    public class UIControl : MonoBehaviour
    {
        public VoxFileDataProvider targetProvider;
        
        public TMP_Dropdown voxInput;
        public TMP_Dropdown destInput;

        public DynSettings[] dynSettings;

        private void Start()
        {
            VoxelManager.Instance.LoadBSA();
            string[] files = BetterStreamingAssets.GetFiles("demo");
            voxInput.options = files.Select(t => new TMP_Dropdown.OptionData(t.Remove(t.Length - 4, 4))).ToList();
            
            voxInput.onValueChanged.AddListener(arg0 => { ReloadB(); });
            destInput.onValueChanged.AddListener(arg0 => { ReloadB(); });
        }

        public void ResetB()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        public void ReloadB()
        {
            VoxCollider[] voxelColliders = FindObjectsOfType<VoxCollider>(true);
            
            foreach (var voxelCollider in voxelColliders)
                Destroy(voxelCollider.gameObject);

            Explosive[] explosives = FindObjectsOfType<Explosive>(true);
            
            foreach (var explosive in explosives)
                Destroy(explosive.gameObject);
            
            DynamicVoxelObj[] voxObjects = FindObjectsOfType<DynamicVoxelObj>(true);
            
            foreach (var dynamicVoxelObj in voxObjects)
            {
                if (!dynamicVoxelObj.GetComponent<VoxFileDataProvider>() && !dynamicVoxelObj.transform.parent.GetComponent<VoxelManager>())
                    Destroy(dynamicVoxelObj.gameObject);
            }

            targetProvider.gameObject.SetActive(true);
            
            targetProvider.GetComponent<DynamicVoxelObj>().dynamicSettings = dynSettings[destInput.value];
            
            string selectVox = voxInput.options[voxInput.value].text;
            targetProvider.modelPath = selectVox;
            targetProvider.ResetVoxelObject();
        }
    }
}