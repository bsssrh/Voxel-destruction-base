using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelDestructionPro.VoxDataProviders;
using VoxelDestructionPro.VoxelObjects;
using VoxReader.Interfaces;

namespace VoxelDestructionPro.Tools
{
    public class VoxCreator : MonoBehaviour
    {
#if UNITY_EDITOR
        public string path;
    
        [Space]
    
        public bool setModelPosition;
    
        [Space] 
    
        public GameObject objectPrefab;
    
        public void Create()
        {
            if (!objectPrefab.GetComponent<VoxFileDataProvider>())
            {
                Debug.LogError("Object prefab does not have a VoxFileDataProvider attached, can not be used!");
                return;
            }
        
            VoxelParser parser = new VoxelParser(path, 0, true);
        
            IModel[] models = parser.GetModels();
        
            for (int i = 0; i < models.Length; i++)
            {
                Vector3 unityPos = new Vector3(models[i].Position.x, models[i].Position.z, models[i].Position.y);
                GameObject n = Instantiate(objectPrefab, transform.position, Quaternion.identity);
                n.transform.parent = transform;
                n.transform.localPosition = setModelPosition ? unityPos : Vector3.zero;
                VoxFileDataProvider vob = n.GetComponent<VoxFileDataProvider>();

                vob.modelPath = path;
                vob.modelIndex = i;
                
                vob.Load(true);
            }
        }
#endif
    }
}