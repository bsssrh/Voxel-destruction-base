using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo
{
    public class ClickToDestroyDemo : MonoBehaviour
    {
        public Slider radiusSlider;
        public TMP_Text radiusTxt;
        public ClickToDestroy clickToDestroy;
        public TMP_InputField modelInput;

        public VoxDataProvider targetProvider;
        
        public float changeSpeed;
    
        public void ResetB()
        {
            if (String.IsNullOrWhiteSpace(modelInput.text))
                return;

            targetProvider.gameObject.SetActive(true);
            if (targetProvider is VoxFileDataProvider fdataProv) 
                fdataProv.modelPath = modelInput.text;
            targetProvider.ResetVoxelObject();
            
            VoxelObjBase[] voxelObjs = FindObjectsOfType<VoxelObjBase>(true);
            VoxelObjBase target = targetProvider.GetComponent<VoxelObjBase>();
            
            for (var i = 0; i < voxelObjs.Length; i++)
            {
                if (voxelObjs[i] == target)
                    continue;
                
                Destroy(voxelObjs[i].gameObject);
            }
        }

        private void Update()
        {
            clickToDestroy.destructionRadius = radiusSlider.value;
            radiusTxt.text = radiusSlider.value.ToString("F1");
        }
    }
}