using UnityEditor;
using UnityEngine;
using VoxelDestructionPro.VoxDataProviders;

namespace VoxelDestructionPro.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VoxDataProvider), true)]
    public class VoxDataProviderInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Load"))
                Load();
    
            if (GUILayout.Button("Clear"))
                Clear();
            
            base.OnInspectorGUI();
        }
        
        public void Load()
        {
            if (Application.isPlaying)
                return;
            
            EditorUtility.DisplayProgressBar("Loading Voxeldata", "This can take some time...", 0.69f);
    
            try
            {
                VoxDataProvider script = (VoxDataProvider)target;
                script.Load(true);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    
        public void Clear()
        {
            if (Application.isPlaying)
                return;
            
            VoxDataProvider script = (VoxDataProvider) target;
            script.Clear();
        }
    }
}