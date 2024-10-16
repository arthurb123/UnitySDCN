using UnityEngine;
using UnityEditor;

namespace UnitySDCN {
    [CustomEditor(typeof(SDCNManager))]
    public class SDCNManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Add a button to the inspector
            if (GUILayout.Button("Render Image")) {
                SDCNManager SDCNManager = (SDCNManager)target;
                SDCNManager.RenderImage();
            }
        }
    }
}
