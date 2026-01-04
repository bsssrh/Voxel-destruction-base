using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Better.StreamingAssets;
using UnityEngine;
using VoxelDestructionPro;
using VoxelDestructionPro.Data;
using VoxReader.Interfaces;

namespace VoxelDestructionPro.Tools
{
    /// <summary>
    /// Reads vox files using the voxreader library,
    /// automatically handles WebGL and BSA
    /// </summary>
    public class VoxelParser
    {
        private string path;
        private int index;
        private bool editorMode;
        
        public VoxelParser(string path, int modelIndex, bool editorMode = false)
        {
            this.path = path;
            index = modelIndex;
            this.editorMode = editorMode;
            
            this.path += ".vox";
        }

        public IModel[] GetModels()
        {
            return ReadModelCountUsingIO();
        }
        
        public VoxelData ParseToVoxelData()
        {
            IModel model;
            
            if (!editorMode)
                model = GetModelFromVoxFile(ReadVoxUsingBSA());
            else
                model = GetModelFromVoxFile(ReadVoxUsingIO());

            if (model == null)
                return null;
            
            return new VoxelData(model);
        }

        public IVoxFile ReadVoxFile()
        {
            if (!editorMode)
                return ReadVoxUsingBSA();
            else
                return ReadVoxUsingIO();
        }

        private IVoxFile ReadVoxUsingBSA()
        {
            VoxelManager.Instance.LoadBSA();
            
            if (VoxelManager.Instance == null)
            {
                Debug.LogError("There is no Voxel Manger in the scene, can't use BSA!");
                return null;
            }
            
            if (!BetterStreamingAssets.FileExists(path))
            {
                Debug.LogError("The file " + path + " does not exist!");
                return null;
            }

            IVoxFile file = VoxReader.VoxReader.Read(path, true);

            return file;
        }
        
        private IVoxFile ReadVoxUsingIO()
        {
            string dataPath = Path.Combine(Application.streamingAssetsPath, path);

            #if !UNITY_WEBGL
            if (!File.Exists(dataPath))
            {
                Debug.LogError("Invalid Path, file does not exist");
                return null;
            }
            #endif            

            IVoxFile file = VoxReader.VoxReader.Read(dataPath, false);

            return file;
        }

        public IModel GetModelFromVoxFile(IVoxFile file)
        {
            if (file == null)
                return null;
            
            IModel model;
            try
            {
                model = file.Models[index];
            }
            catch (IndexOutOfRangeException)
            {
                Debug.LogError("Invalid modelIndex, your index is too high and is not present in the vox file!");
                return null;
            }

            return model;
        }
        
        private IModel[] ReadModelCountUsingIO()
        {
            path = Path.Combine(Application.streamingAssetsPath, path);

            if (!File.Exists(path))
                throw new InvalidOperationException();
            
            IVoxFile file = VoxReader.VoxReader.Read(path, false);
            
            return file.Models;
        }
    }
}

