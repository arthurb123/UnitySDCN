#nullable enable

using System;
using UnityEngine;

namespace UnitySDCN {
    [RequireComponent(typeof(MeshFilter))]
    public class SDCNObject : MonoBehaviour {
        public string Description => _description;

        [Header("Settings")]
        [TextArea(3, 10)]
        [SerializeField] private string _description = string.Empty;

        public AABB? GetBoundingBox() {
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
            Bounds bounds = mesh.bounds;

            // Get the min and max points
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            // Return the AABB
            return new AABB(min, max);
        }
    }

    [Serializable]
    public class AABB {
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }

        public AABB(Vector3 min, Vector3 max) {
            Min = min;
            Max = max;
        }
    }
}