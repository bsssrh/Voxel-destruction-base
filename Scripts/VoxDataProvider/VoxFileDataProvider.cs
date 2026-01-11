using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Tools;
using VoxReader.Interfaces;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelDestructionPro.VoxDataProviders
{
    public class VoxFileDataProvider : VoxDataProvider
    {
        [Header("Source (choose ONE)")]
        [Tooltip("Drag & drop a .vox file here. If assigned, it has priority over Model Path.")]
        public DefaultAsset voxFile; // Editor: .vox как DefaultAsset

        [Header("Path (fallback)")]
        [Tooltip("Relative path (without extension) inside StreamingAssets, e.g. 'Models/character'")]
        public string modelPath;

        [Tooltip("Vox files can be split into multiple models (MagicaVoxel layers)")]
        public int modelIndex;

        [Tooltip("If enabled the vox file will only be read once and then cached and reused")]
        public bool useModelCaching = true;

        // cache: assetInstanceId + modelIndex
        private static readonly Dictionary<long, VoxelData> _assetCache = new Dictionary<long, VoxelData>();

#if UNITY_EDITOR
        private int _lastAutoLoadHash;
        private bool _autoLoadScheduled;
#endif

        public override void Load(bool editorMode)
        {
            base.Load(editorMode);

            // Никаких спам-логов: просто тихо выходим, если нельзя загрузить
            if (targetObj == null)
                return;

            // Не допускаем отрицательных значений — тихо фикс
            if (modelIndex < 0)
                modelIndex = 0;

            // 1) Priority: voxFile (Editor-only чтение DefaultAsset)
            if (voxFile != null)
            {
                if (TryLoadFromVoxAsset(editorMode))
                    return;
                // НЕ логируем “failed”, просто молча падаем на fallback
            }

            // 2) Fallback: modelPath (оригинальная логика)
            if (string.IsNullOrWhiteSpace(modelPath))
                return;

#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(LoadFileUsingWebRequest());
#else
            if (!useModelCaching || editorMode)
            {
                VoxelParser parser = new VoxelParser(modelPath, modelIndex, editorMode);
                VoxelData vox = null;

                try
                {
                    vox = parser.ParseToVoxelData();
                }
                catch
                {
                    // тихо
                    vox = null;
                }

                if (vox == null)
                    return;

                targetObj.AssignVoxelData(vox, editorMode);
            }
            else
            {
                VoxelData cached = null;
                try
                {
                    cached = VoxelManager.Instance.LoadAndCacheVoxFile(modelPath, modelIndex);
                }
                catch
                {
                    cached = null;
                }

                if (cached == null)
                    return;

                targetObj.AssignVoxelData(cached, editorMode);
            }
#endif
        }

        private bool TryLoadFromVoxAsset(bool editorMode)
        {
#if !UNITY_EDITOR
            // В билде DefaultAsset не читается в байты — тихо
            return false;
#else
            if (voxFile == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(voxFile);
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.EndsWith(".vox", StringComparison.OrdinalIgnoreCase))
                return false;

            // Ключ кеша зависит от индекса, но индекс мы можем клэмпить после чтения —
            // поэтому кеш используем только если индекс уже валиден.
            long cacheKey = ((long)voxFile.GetInstanceID() << 32) ^ (uint)Mathf.Max(0, modelIndex);

            if (useModelCaching && !editorMode && _assetCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                targetObj.AssignVoxelData(cached, editorMode);
                return true;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(assetPath);
            }
            catch
            {
                return false;
            }

            if (bytes == null || bytes.Length == 0)
                return false;

            IVoxFile file;
            try
            {
                file = VoxReader.VoxReader.Read(bytes);
            }
            catch
            {
                return false;
            }

            if (file?.Models == null || file.Models.Length == 0)
                return false;

            // ТИХО клэмпим modelIndex в допустимый диапазон
            int maxIndex = file.Models.Length - 1;
            if (modelIndex < 0) modelIndex = 0;
            if (modelIndex > maxIndex) modelIndex = maxIndex;

            VoxelData data;
            try
            {
                data = new VoxelData(file.Models[modelIndex]);
            }
            catch
            {
                return false;
            }

            if (data == null)
                return false;

            targetObj.AssignVoxelData(data, editorMode);

            if (useModelCaching && !editorMode)
            {
                long finalKey = ((long)voxFile.GetInstanceID() << 32) ^ (uint)modelIndex;
                _assetCache[finalKey] = data;
            }

            return true;
#endif
        }

        private IEnumerator LoadFileUsingWebRequest()
        {
            // WebGL: только StreamingAssets путь
            string dataPath = Path.Combine(Application.streamingAssetsPath, modelPath + ".vox");

            UnityWebRequest request = UnityWebRequest.Get(dataPath);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success || request.downloadHandler.data == null)
                yield break;

            IVoxFile file;
            try
            {
                file = VoxReader.VoxReader.Read(request.downloadHandler.data);
            }
            catch
            {
                yield break;
            }

            if (file?.Models == null || file.Models.Length == 0)
                yield break;

            int maxIndex = file.Models.Length - 1;
            if (modelIndex < 0) modelIndex = 0;
            if (modelIndex > maxIndex) modelIndex = maxIndex;

            VoxelData data;
            try
            {
                data = new VoxelData(file.Models[modelIndex]);
            }
            catch
            {
                yield break;
            }

            if (data == null)
                yield break;

            targetObj.AssignVoxelData(data);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // хэш изменений
            int h = 17;
            h = h * 31 + (voxFile ? voxFile.GetInstanceID() : 0);
            h = h * 31 + (modelPath != null ? modelPath.GetHashCode() : 0);
            h = h * 31 + modelIndex;
            h = h * 31 + (useModelCaching ? 1 : 0);

            if (h == _lastAutoLoadHash)
                return;

            _lastAutoLoadHash = h;

            if (!isActiveAndEnabled)
                return;

            if (_autoLoadScheduled)
                return;

            _autoLoadScheduled = true;

            // ВАЖНО: delayCall, чтобы не грузить в середине сериализации инспектора
            EditorApplication.delayCall += () =>
            {
                _autoLoadScheduled = false;

                if (this == null) return;
                if (!isActiveAndEnabled) return;

                try
                {
                    Load(true);
                }
                catch
                {
                    // Никакого спама — полностью тихо
                }
            };
        }
#endif
    }
}
