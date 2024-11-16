#nullable enable

using System;
using UnityEngine;

namespace UnitySDCN {
    [RequireComponent(typeof(MeshFilter))]
    public class SDCNObject : MonoBehaviour {
        [Header("Settings")]
        [TextArea(3, 10)]
        public string Description = string.Empty;

        public Bounds? GetBounds() {
            // Get the mesh filter component
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) {
                SDCNLogger.Error(
                    typeof(SDCNObject), 
                    $"Could not get bounding box, no mesh filter found for SDCNObject {gameObject.name}!"
                );
                return null;
            }

            // Get the mesh
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) {
                SDCNLogger.Error(
                    typeof(SDCNObject), 
                    $"Could not get bounding box, shared mesh not set for SDCNObject {gameObject.name}!"
                );
                return null;
            }

            // Get the bounds
            return mesh.bounds;
        }
    }
}