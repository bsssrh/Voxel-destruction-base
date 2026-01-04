#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VoxelObjBase), true)]
    public class VoxelBaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Quick setup"))
                CreateMeshObj();
            
            base.OnInspectorGUI();
        }
    
        public void CreateMeshObj()
        {
            if (Application.isPlaying)
                return;
            
            VoxelObjBase script = (VoxelObjBase) target;
            VoxelManager manager = FindObjectOfType<VoxelManager>();
            script.QuickSetup(manager);
        }
    }
}
#endif