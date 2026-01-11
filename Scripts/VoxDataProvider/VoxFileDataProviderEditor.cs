#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VoxelDestructionPro.VoxDataProviders;
using VoxReader.Interfaces;

public class VoxModelInfoCache
{
    private struct Info
    {
        public int modelCount;
        public DateTime lastWriteUtc;
    }

    private static readonly Dictionary<string, Info> _cache = new Dictionary<string, Info>(256);

    public static int GetModelCount(DefaultAsset voxAsset)
    {
        if (voxAsset == null) return 0;

        string path = AssetDatabase.GetAssetPath(voxAsset);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".vox", StringComparison.OrdinalIgnoreCase))
            return 0;

        try
        {
            var fi = new FileInfo(path);
            if (!fi.Exists) return 0;

            DateTime w = fi.LastWriteTimeUtc;

            if (_cache.TryGetValue(path, out var info) && info.lastWriteUtc == w)
                return info.modelCount;

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0) return 0;

            IVoxFile file = VoxReader.VoxReader.Read(bytes);
            int count = file?.Models != null ? file.Models.Length : 0;

            _cache[path] = new Info { modelCount = count, lastWriteUtc = w };
            return count;
        }
        catch
        {
            return 0;
        }
    }
}

[CustomEditor(typeof(VoxFileDataProvider))]
public class VoxFileDataProviderEditor : Editor
{
    SerializedProperty voxFileProp;
    SerializedProperty modelPathProp;
    SerializedProperty modelIndexProp;
    SerializedProperty useModelCachingProp;

    private void OnEnable()
    {
        voxFileProp = serializedObject.FindProperty("voxFile");
        modelPathProp = serializedObject.FindProperty("modelPath");
        modelIndexProp = serializedObject.FindProperty("modelIndex");
        useModelCachingProp = serializedObject.FindProperty("useModelCaching");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Source (choose ONE)", EditorStyles.boldLabel);
        DrawVoxOnlyField();

        // Slider для modelIndex, если voxFile задан и у него есть модели
        var vox = voxFileProp.objectReferenceValue as DefaultAsset;
        int modelCount = VoxModelInfoCache.GetModelCount(vox);

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(modelPathProp);

        if (vox != null && modelCount > 0)
        {
            int maxIndex = modelCount - 1;
            int cur = Mathf.Clamp(modelIndexProp.intValue, 0, maxIndex);

            EditorGUI.BeginChangeCheck();
            cur = EditorGUILayout.IntSlider(new GUIContent($"Model Index (0..{maxIndex})"), cur, 0, maxIndex);
            if (EditorGUI.EndChangeCheck())
                modelIndexProp.intValue = cur;

            EditorGUILayout.LabelField($"Models in file: {modelCount}", EditorStyles.miniLabel);
        }
        else
        {
            // если voxFile нет, оставляем обычное поле
            EditorGUILayout.PropertyField(modelIndexProp);
        }

        EditorGUILayout.PropertyField(useModelCachingProp);
        EditorGUILayout.HelpBox("Priority: VoxFile first, then Model Path.\nEditor UX: click a .vox in picker list to assign instantly.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();

        // Жёсткая защита: только .vox в поле
        ValidateVoxFieldAndClampIndex();
    }

    private void DrawVoxOnlyField()
    {
        Rect r = EditorGUILayout.GetControlRect(true);

        Rect labelRect = r;
        labelRect.width = EditorGUIUtility.labelWidth;
        EditorGUI.LabelField(labelRect, "Vox File (.vox)");

        Rect fieldRect = r;
        fieldRect.xMin += EditorGUIUtility.labelWidth;

        var current = voxFileProp.objectReferenceValue as DefaultAsset;
        string text = current ? current.name : "None (.vox only)";

        if (GUI.Button(fieldRect, text, EditorStyles.objectField))
        {
            VoxOnlyPickerWindow.Open(
                current: current,
                onPicked: (picked) =>
                {
                    voxFileProp.objectReferenceValue = picked;
                    serializedObject.ApplyModifiedProperties();

                    // После выбора — автоматически нормализуем modelIndex под этот файл
                    ClampModelIndexForCurrentVox();

                    // Авто-LOAD в editor (но безопасно делать только если ты уже добавил guard в VoxelObjBase)
                    // Если guard ещё не добавлен — лучше не включать.
                    try
                    {
                        var p = (VoxFileDataProvider)target;
                        if (p != null && p.isActiveAndEnabled)
                            p.Load(true);
                    }
                    catch { /* тихо, без спама */ }
                }
            );
        }
    }

    private void ValidateVoxFieldAndClampIndex()
    {
        var obj = voxFileProp.objectReferenceValue;
        if (obj == null) return;

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".vox", StringComparison.OrdinalIgnoreCase))
        {
            voxFileProp.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            return;
        }

        ClampModelIndexForCurrentVox();
    }

    private void ClampModelIndexForCurrentVox()
    {
        var vox = voxFileProp.objectReferenceValue as DefaultAsset;
        if (vox == null) return;

        int modelCount = VoxModelInfoCache.GetModelCount(vox);
        if (modelCount <= 0) return;

        int max = modelCount - 1;
        int cur = modelIndexProp.intValue;
        int clamped = Mathf.Clamp(cur, 0, max);

        if (clamped != cur)
        {
            modelIndexProp.intValue = clamped;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

public class VoxOnlyPickerWindow : EditorWindow
{
    private Action<DefaultAsset> _onPicked;
    private Vector2 _scroll;
    private string _search = "";
    private List<DefaultAsset> _voxAssets = new List<DefaultAsset>();
    private DefaultAsset _selected;

    public static void Open(DefaultAsset current, Action<DefaultAsset> onPicked)
    {
        var w = CreateInstance<VoxOnlyPickerWindow>();
        w.titleContent = new GUIContent("Vox Picker (.vox only)");
        w.minSize = new Vector2(520, 520);
        w._selected = current;
        w._onPicked = onPicked;
        w.RefreshList();
        w.ShowUtility();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Search", GUILayout.Width(50));
            string newSearch = EditorGUILayout.TextField(_search);
            if (newSearch != _search)
            {
                _search = newSearch;
                RefreshList();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                RefreshList();

            if (GUILayout.Button("Close", GUILayout.Width(70)))
                Close();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Found: {_voxAssets.Count} (.vox)", EditorStyles.miniLabel);
        EditorGUILayout.Space(6);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < _voxAssets.Count; i++)
        {
            var a = _voxAssets[i];
            if (a == null) continue;

            string path = AssetDatabase.GetAssetPath(a);

            // Row
            Rect row = EditorGUILayout.BeginVertical("box");
            bool isSel = (_selected == a);

            using (new EditorGUILayout.HorizontalScope())
            {
                // КНОПКА: клик = мгновенно назначить, окно НЕ закрывается
                GUIStyle s = new GUIStyle(EditorStyles.miniButton);
                if (isSel) s.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(a.name, s, GUILayout.Height(22)))
                {
                    _selected = a;
                    _onPicked?.Invoke(a);
                    Repaint();
                }

            }

            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Double click — тоже назначает (и тоже НЕ закрывает)
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && row.Contains(Event.current.mousePosition))
            {
                _selected = a;
                _onPicked?.Invoke(a);
                Event.current.Use();
                Repaint();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshList()
    {
        _voxAssets.Clear();

        // Сканим проект и берём только .vox
        string[] guids = AssetDatabase.FindAssets("t:DefaultAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(p)) continue;
            if (!p.EndsWith(".vox", StringComparison.OrdinalIgnoreCase)) continue;

            string name = Path.GetFileNameWithoutExtension(p);
            if (!string.IsNullOrEmpty(_search) && !name.ToLowerInvariant().Contains(_search.ToLowerInvariant()))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(p);
            if (asset != null)
                _voxAssets.Add(asset);
        }

        _voxAssets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        Repaint();
    }
}
#endif
