#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelDestructionPro.Tools;

namespace VoxelDestructionPro.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VoxCreator))]
    public class VoxCreatorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Delete the old ones first, before creating again!", MessageType.Info);
        
            if (GUILayout.Button("Precreate"))
                Precreate();

            base.OnInspectorGUI();
        }

        public void Precreate()
        {
            if (Application.isPlaying)
                return;
        
            VoxCreator script = (VoxCreator) target;
            script.Create();
        }
    }
}
#endif
