using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Tools;
using VoxReader.Interfaces;

namespace VoxelDestructionPro.VoxDataProviders
{
    public class VoxFileDataProvider : VoxDataProvider
    {
        public string modelPath;
        [Tooltip("Vox files can be split into multiple models (look at MagicaVoxel layers section)")]
        public int modelIndex;
        [Tooltip("If enabled the vox file will only be read once and then cached and reused")]
        public bool useModelCaching = true;

        public override void Load(bool editorMode)
        {
            base.Load(editorMode);
            
            if (String.IsNullOrWhiteSpace(modelPath))
            {
                Debug.LogWarning("Model path is empty!", this);
                return;
            }

            if (modelIndex < 0)
            {
                Debug.LogWarning("Model index can not be smaller than zero!", this);
                return;
            }
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(LoadFileUsingWebRequest());
            #else
            if (!useModelCaching || editorMode)
            {
                VoxelParser parser = new VoxelParser(modelPath, modelIndex, editorMode);
                VoxelData vox = parser.ParseToVoxelData();
                
                if (vox == null)
                    return;
                
                targetObj.AssignVoxelData(vox, editorMode);
            }
            else
                targetObj.AssignVoxelData(VoxelManager.Instance.LoadAndCacheVoxFile(modelPath, modelIndex));
            #endif
        }

        /// <summary>
        /// Webgl forces you to use WebRequests to losd files
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadFileUsingWebRequest()
        {
            string dataPath = Path.Combine(Application.streamingAssetsPath, modelPath + ".vox");
            
            UnityWebRequest request = UnityWebRequest.Get(dataPath);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success || request.downloadHandler.data == null)
            {
                Debug.LogError("Error reading vox file", this);
                yield break;
            }
                
            IVoxFile file = VoxReader.VoxReader.Read(request.downloadHandler.data);
            targetObj.AssignVoxelData(new VoxelData(file.Models[modelIndex]));
        }
    }
}