#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnitySDCN
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    public class SDCNCamera : MonoBehaviour
    {
        public string BackgroundDescription => _backgroundDescription;
        public string NegativeDescription => _negativeDescription;
        public LayerMask ExcludeLayers => _excludeLayers;

        [Header("Settings")]
        [SerializeField] private LayerMask _excludeLayers = 0;
        [SerializeField] [Range(0.1f, 4f)] private float captureScale = 1f;
        [Tooltip("Applies to all objects in the scene which do not have a SDCNObject component.")]
        [SerializeField] [TextArea(3, 10)] private string _backgroundDescription = string.Empty;
        [Tooltip("Applies to all objects in the scene, including the background.")]
        [SerializeField] [TextArea(3, 10)] private string _negativeDescription = string.Empty;

        private Camera? _camera;
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /**
          * Capture a depth image from the camera
          * 
          * @return A byte array containing the depth image in PNG format
          */
        internal byte[]? CaptureDepthImage()
        {
            if (_camera == null)
            {
                SDCNLogger.Error(
                    typeof(SDCNCamera), 
                    "Could not capture depth image, no camera found in SDCNCamera!"
                );
                return null;
            }
            SDCNLogger.Log(
                typeof(SDCNManager), 
                $"Capturing depth image from camera {_camera.name}.", 
                SDCNVerbosity.Minimal
            );

            Vector2Int captureResolution = GetCaptureResolution();
            float originalAspect = _camera.aspect;
            _camera.aspect = (float)captureResolution.x / captureResolution.y;

            RenderTexture depthRT = new(captureResolution.x, captureResolution.y, 24, RenderTextureFormat.Depth);
            RenderTexture colorRT = new(captureResolution.x, captureResolution.y, 0, RenderTextureFormat.ARGB32);
            Material depthMaterial = new(Shader.Find("UnitySDCN/Depth"));

            RenderTexture originalRT = _camera.targetTexture;
            CameraClearFlags originalClearFlags = _camera.clearFlags;

            try
            {
                // Set up camera for depth rendering
                _camera.targetTexture = depthRT;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = Color.white;

                // Render the camera
                _camera.Render();

                // Blit depth texture to color texture
                Graphics.Blit(depthRT, colorRT, depthMaterial);

                // Read pixels from the color texture
                Texture2D depthTexture = new(captureResolution.x, captureResolution.y, TextureFormat.RGB24, false);
                RenderTexture.active = colorRT;
                depthTexture.ReadPixels(new Rect(0, 0, captureResolution.x, captureResolution.y), 0, 0);
                depthTexture.Apply();

                byte[] bytes = depthTexture.EncodeToPNG();
                SDCNLogger.Log(
                    typeof(SDCNManager), 
                    "Depth image captured successfully.", 
                    SDCNVerbosity.Verbose
                );
                return bytes;
            }
            finally
            {
                // Restore camera settings
                _camera.aspect = originalAspect;
                _camera.targetTexture = originalRT;
                _camera.clearFlags = originalClearFlags;

                RenderTexture.active = null;
                if (depthRT != null) 
                    depthRT.Release();
                if (colorRT != null) 
                    colorRT.Release();
                if (depthMaterial != null) 
                    Destroy(depthMaterial);
            }
        }

        /**
          * Capture a segmentation image from the camera
          * 
          * @return An array of all masked segments in the image which
          *         contain a SDCNObject component
          */
        internal SDCNSegment[]? CaptureSegmentedImage() {
            if (_camera == null) {
                SDCNLogger.Error(
                    typeof(SDCNCamera), 
                    "Could not capture masked images, no camera found in SDCNCamera!"
                );
                return null;
            }

            SDCNLogger.Log(
                typeof(SDCNManager), 
                $"Capturing masked images from camera {_camera.name}.", 
                SDCNVerbosity.Minimal
            );

            Vector2Int captureResolution = GetCaptureResolution();
            float originalAspect = _camera.aspect;
            _camera.aspect = (float)captureResolution.x / captureResolution.y;

            Volume[] globalVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            float[] originalGlobalVolumeWeights = new float[globalVolumes.Length];
            foreach (Volume volume in globalVolumes) {
                originalGlobalVolumeWeights[Array.IndexOf(globalVolumes, volume)] = volume.weight;
                volume.weight = 0.0f;
            }

            Renderer[] renderers = new Renderer[0];
            Dictionary<Renderer, (bool, ShadowCastingMode)> originalShading = new();
            Dictionary<Renderer, Material[]> originalMaterials = new();
            List<SDCNSegment> segments = new();

            Material? originalSkybox = RenderSettings.skybox;
            Color originalBackgroundColor = _camera.backgroundColor;
            CameraClearFlags originalClearFlags = _camera.clearFlags;
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientLight = RenderSettings.ambientLight;
            RenderTexture? originalTargetTexture = _camera.targetTexture;

            RenderTexture? maskTexture = null;
            Material? skyboxMaterial = null;
            Material? whiteMaterial = null;
            Material? blackMaterial = null;

            try {
                renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                SDCNLogger.Log(
                    typeof(SDCNManager), 
                    $"Found {renderers.Length} renderers in the scene.", 
                    SDCNVerbosity.Verbose
                );

                foreach (Renderer renderer in renderers) {
                    originalShading[renderer] = (renderer.receiveShadows, renderer.shadowCastingMode);
                    renderer.receiveShadows = false;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    originalMaterials[renderer] = renderer.sharedMaterials;
                }

                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) {
                    SDCNLogger.Error(
                        typeof(SDCNManager), 
                        "Shader 'Universal Render Pipeline/Unlit' not found!", 
                        SDCNVerbosity.Minimal
                    );
                    return null;
                }
                whiteMaterial = new Material(unlitShader) { color = Color.white };
                blackMaterial = new Material(unlitShader) { color = Color.black };

                skyboxMaterial = new Material(unlitShader) { color = Color.black };
                RenderSettings.skybox = skyboxMaterial;

                _camera.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;

                maskTexture = new RenderTexture(captureResolution.x, captureResolution.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                maskTexture.Create();

                foreach (Renderer renderer in renderers) {
                    SDCNObject? sdcnObject = renderer.GetComponent<SDCNObject>();
                    if (sdcnObject == null) continue;

                    foreach (Renderer r in renderers) {
                        r.material = blackMaterial;
                    }
                    renderer.material = whiteMaterial;

                    _camera.targetTexture = maskTexture;
                    _camera.Render();

                    Texture2D maskImage = new(captureResolution.x, captureResolution.y, TextureFormat.ARGB32, false);
                    RenderTexture.active = maskTexture;
                    maskImage.ReadPixels(new Rect(0, 0, captureResolution.x, captureResolution.y), 0, 0);
                    maskImage.Apply();
                    RenderTexture.active = null;

                    segments.Add(new SDCNSegment(maskImage.EncodeToPNG(), sdcnObject));

                    // Clean up the temporary texture
                    DestroyImmediate(maskImage);
                }

                return segments.ToArray();
            }
            finally {
                // Restore original materials and shading
                foreach (var kvp in originalMaterials) {
                    if (kvp.Key != null) {
                        kvp.Key.sharedMaterials = kvp.Value;
                    }
                }
                foreach (Renderer renderer in renderers) {
                    if (originalShading.TryGetValue(renderer, out var shading)) {
                        renderer.receiveShadows = shading.Item1;
                        renderer.shadowCastingMode = shading.Item2;
                    }
                }

                // Restore original camera and render settings
                _camera.clearFlags = originalClearFlags;
                _camera.backgroundColor = originalBackgroundColor;
                _camera.targetTexture = originalTargetTexture;
                _camera.aspect = originalAspect;
                RenderSettings.skybox = originalSkybox;
                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;

                // Restore the volume weights
                for (int i = 0; i < globalVolumes.Length; i++) {
                    globalVolumes[i].weight = originalGlobalVolumeWeights[i];
                }

                // Clean up
                if (maskTexture != null) {
                    maskTexture.Release();
                    DestroyImmediate(maskTexture);
                }
                if (skyboxMaterial != null) DestroyImmediate(skyboxMaterial);
                if (whiteMaterial != null) DestroyImmediate(whiteMaterial);
                if (blackMaterial != null) DestroyImmediate(blackMaterial);

                // Ensure RenderTexture.active is set to null
                RenderTexture.active = null;
            }
        }

        private Vector2Int GetCaptureResolution()
        {
            if (_camera == null)
            {
                SDCNLogger.Error(
                    typeof(SDCNCamera),
                    "Could not get capture resolution, no camera found in SDCNCamera!"
                );
                return Vector2Int.zero;
            }

            int originalWidth = Mathf.RoundToInt(_camera.pixelWidth * captureScale);
            int originalHeight = Mathf.RoundToInt(_camera.pixelHeight * captureScale);

            // Adjust width to the closest power of two
            int width = Mathf.ClosestPowerOfTwo(originalWidth);
            // Calculate height based on the original aspect ratio
            int height = Mathf.RoundToInt(width * ((float)originalHeight / originalWidth));

            return new Vector2Int(width, height);
        }
    }
}
