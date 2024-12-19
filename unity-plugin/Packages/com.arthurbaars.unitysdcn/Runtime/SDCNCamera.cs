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
        public SDCNCameraMode Mode => _mode;

        [Header("Settings")]
        [SerializeField] private SDCNCameraMode _mode = 
            SDCNCameraMode.Segmented 
        |   SDCNCameraMode.Depth 
        |   SDCNCameraMode.Normal;
        [SerializeField] [Range(0.1f, 4f)] private float captureScale = 1f;
        [Tooltip("Applies to all objects in the scene which do not have a SDCNObject component.")]
        [SerializeField] [TextArea(3, 10)] private string _backgroundDescription = string.Empty;
        [Tooltip("Applies to all objects in the scene, including the background.")]
        [SerializeField] [TextArea(3, 10)] private string _negativeDescription = string.Empty;

        private Camera? _camera;
        
        private void Awake()
        {
            // Get the camera component
            _camera = GetComponent<Camera>();
        }

        /**
            * Capture various image data from the camera, based
            * on the enabled camera modes
            *
            * @return A SDCNCameraCapture object containing the captured images
            */
        public SDCNCameraCapture? Capture() {
            // Get the capture resolution
            Vector2Int captureResolution = GetCaptureResolution();

            // Check if camera mode is set to none, segments
            // must ALWAYS be captured
            if (_mode == 0) {
                SDCNLogger.Warning(
                    typeof(SDCNCamera), 
                    "Found no camera modes enabled, defaulting to Segmented mode."
                );
                _mode = SDCNCameraMode.Segmented;
            }

            // Log modes
            SDCNLogger.Log(
                typeof(SDCNCamera), 
                $"Capturing images with modes: {_mode}.", 
                SDCNVerbosity.Verbose
            );

            // Capture segments by default
            SDCNSegment[]? segments = CaptureSegmentedImage(captureResolution);
            if (segments == null)
                return null;

            // Optionally capture depth and normal images
            byte[]? depthImage = null;
            if ((_mode & SDCNCameraMode.Depth) != 0)
                depthImage = CaptureDepthImage(captureResolution);
            byte[]? normalImage = null;
            if ((_mode & SDCNCameraMode.Normal) != 0)
                normalImage = CaptureNormalImage(captureResolution);

            // Return the camera capture
            return new SDCNCameraCapture(
                width: captureResolution.x,
                height: captureResolution.y,
                segments: segments,
                depthImage: depthImage,
                normalImage: normalImage
            );
        }

        /**
          * Capture a depth image from the camera
          * 
          * @param captureResolution The resolution of the image to capture in pixels
          * @return A byte array containing the depth image in PNG format
          */
        public byte[]? CaptureDepthImage(Vector2Int captureResolution)
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
                SDCNVerbosity.Verbose
            );

            float originalAspect = _camera.aspect;
            _camera.aspect = (float)captureResolution.x / captureResolution.y;

            RenderTexture depthRT = new(captureResolution.x, captureResolution.y, 24, RenderTextureFormat.Depth);
            RenderTexture colorRT = new(captureResolution.x, captureResolution.y, 0, RenderTextureFormat.ARGB32);

            Shader depthShader = Shader.Find("SDCN/Depth");
            if ( depthShader == null)
            {
                SDCNLogger.Error(
                    typeof(SDCNManager),
                    "Shader 'SDCN/Depth' not found!"
                );
                return null;
            }
            Material depthMaterial = new(depthShader);

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

                // Normalize the depth texture
                Color[] pixels = depthTexture.GetPixels();
                float maxDepth = 0.0f;
                foreach (Color pixel in pixels)
                    maxDepth = Mathf.Max(maxDepth, pixel.r);
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(pixels[i].r / maxDepth, pixels[i].r / maxDepth, pixels[i].r / maxDepth);
                depthTexture.SetPixels(pixels);
                depthTexture.Apply();

                byte[] bytes = depthTexture.EncodeToPNG();
                SDCNLogger.Log(
                    typeof(SDCNManager), 
                    "Depth image captured successfully.", 
                    SDCNVerbosity.Minimal
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
            * Capture a normal image from the camera
            * 
            * @param captureResolution The resolution of the image to capture in pixels
            * @return A byte array containing the normal image in PNG format
            */
        public byte[]? CaptureNormalImage(Vector2Int captureResolution)
        {
            if (_camera == null)
            {
                SDCNLogger.Error(
                    typeof(SDCNCamera),
                    "Could not capture normal image, no camera found in SDCNCamera!"
                );
                return null;
            }

            SDCNLogger.Log(
                typeof(SDCNManager),
                $"Capturing normal image from camera {_camera.name}.",
                SDCNVerbosity.Verbose
            );

            float originalAspect = _camera.aspect;
            _camera.aspect = (float)captureResolution.x / captureResolution.y;

            // Store global volume weights and disable them
            Volume[] globalVolumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            float[] originalGlobalVolumeWeights = new float[globalVolumes.Length];
            for (int i = 0; i < globalVolumes.Length; i++)
            {
                originalGlobalVolumeWeights[i] = globalVolumes[i].weight;
                globalVolumes[i].weight = 0.0f;
            }

            Renderer[] renderers = new Renderer[0];
            Dictionary<Renderer, (bool, ShadowCastingMode)> originalShading = new();
            Dictionary<Renderer, Material[]> originalMaterials = new();

            Material? originalSkybox = RenderSettings.skybox;
            Color originalBackgroundColor = _camera.backgroundColor;
            CameraClearFlags originalClearFlags = _camera.clearFlags;
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientLight = RenderSettings.ambientLight;
            RenderTexture? originalTargetTexture = _camera.targetTexture;

            RenderTexture? normalRT = null;
            Material? skyboxMaterial = null;
            Material? normalMaterial = null;

            try
            {
                // Find all renderers in the scene
                renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                SDCNLogger.Log(
                    typeof(SDCNManager),
                    $"Found {renderers.Length} renderers in the scene.",
                    SDCNVerbosity.Verbose
                );

                // Store original shading and materials
                foreach (Renderer renderer in renderers)
                {
                    originalShading[renderer] = (renderer.receiveShadows, renderer.shadowCastingMode);
                    renderer.receiveShadows = false;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    originalMaterials[renderer] = renderer.sharedMaterials;
                }

                // Load the normal shader
                Shader normalShader = Shader.Find("SDCN/Normals");
                if (normalShader == null)
                {
                    SDCNLogger.Error(
                        typeof(SDCNManager),
                        "Shader 'SDCN/Normals' not found!"
                    );
                    return null;
                }

                // Create the normal material
                normalMaterial = new Material(normalShader);

                // Set up a black skybox
                skyboxMaterial = new Material(Shader.Find("SDCN/Unlit"));
                skyboxMaterial.color = Color.black;
                RenderSettings.skybox = skyboxMaterial;

                _camera.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;

                // Create a RenderTexture for capturing the normal image
                normalRT = new RenderTexture(captureResolution.x, captureResolution.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                normalRT.Create();

                // Assign the normal material to all renderers
                foreach (Renderer renderer in renderers)
                {
                    Material[] normalMaterials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < normalMaterials.Length; i++)
                    {
                        normalMaterials[i] = normalMaterial;
                    }
                    renderer.sharedMaterials = normalMaterials;
                }

                // Set up camera for normal rendering
                _camera.targetTexture = normalRT;

                // Render the camera
                _camera.Render();

                // Read pixels from the normal texture
                Texture2D normalTexture = new Texture2D(captureResolution.x, captureResolution.y, TextureFormat.ARGB32, false);
                RenderTexture.active = normalRT;
                normalTexture.ReadPixels(new Rect(0, 0, captureResolution.x, captureResolution.y), 0, 0);
                normalTexture.Apply();
                RenderTexture.active = null;

                // Encode the captured texture to PNG
                byte[] bytes = normalTexture.EncodeToPNG();

                // Clean up the temporary texture
                DestroyImmediate(normalTexture);

                SDCNLogger.Log(
                    typeof(SDCNManager),
                    "Normal image captured successfully.",
                    SDCNVerbosity.Minimal
                );

                return bytes;
            }
            finally
            {
                // Restore original materials and shading
                foreach (var kvp in originalMaterials)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.sharedMaterials = kvp.Value;
                    }
                }
                foreach (Renderer renderer in renderers)
                {
                    if (originalShading.TryGetValue(renderer, out var shading))
                    {
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
                for (int i = 0; i < globalVolumes.Length; i++)
                {
                    globalVolumes[i].weight = originalGlobalVolumeWeights[i];
                }

                // Clean up
                if (normalRT != null)
                {
                    normalRT.Release();
                    DestroyImmediate(normalRT);
                }
                if (skyboxMaterial != null) DestroyImmediate(skyboxMaterial);
                if (normalMaterial != null) DestroyImmediate(normalMaterial);

                // Ensure RenderTexture.active is set to null
                RenderTexture.active = null;

                SDCNLogger.Log(
                    typeof(SDCNManager),
                    "Restored original settings after capturing normal image.",
                    SDCNVerbosity.Verbose
                );
            }
        }

        /**
          * Capture a segmentation image from the camera
          * 
          * @param captureResolution The resolution of the image to capture in pixels
          * @return An array of all masked segments in the image which
          *         contain a SDCNObject component
          */
        public SDCNSegment[]? CaptureSegmentedImage(Vector2Int captureResolution) {
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
                SDCNVerbosity.Verbose
            );

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

                Shader unlitShader = Shader.Find("SDCN/Unlit");
                if (unlitShader == null) {
                    SDCNLogger.Error(
                        typeof(SDCNManager), 
                        "Shader 'SDCN/Unlit' not found!"
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
                    if (sdcnObject == null)
                        continue;

                    foreach (Renderer r in renderers)
                        r.material = blackMaterial;
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

                // We want to sort segments on their SDCNObject
                // bounding boxes such that closer objects are
                // rendered first
                Vector3 cameraPosition = _camera.transform.position;
                segments.Sort((a, b) => {
                    Bounds? boundsA = a.SDCNObject.GetBounds();
                    Bounds? boundsB = b.SDCNObject.GetBounds();
                    if (boundsA == null || boundsB == null) 
                        return 0;

                    float distanceA = Vector3.Distance(cameraPosition, boundsA.Value.center);
                    float distanceB = Vector3.Distance(cameraPosition, boundsB.Value.center);
                    return distanceA.CompareTo(distanceB);
                });

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

                SDCNLogger.Log(
                    typeof(SDCNManager), 
                    "Captured image segments successfully.",
                    SDCNVerbosity.Minimal
                );
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

    // A camera capture which contains various
    // images, based on the enabled camera modes
    public class SDCNCameraCapture {
        // The width of the captured images in pixels
        public int Width { get; private set; }
        // The height of the captured images in pixels
        public int Height { get; private set; }
        // The segmented images
        public SDCNSegment[] Segments { get; private set; }
        // The optional depth image
        public byte[]? DepthImage { get; private set; }
        // The optional normal image
        public byte[]? NormalImage { get; private set; }

        /**
            * Create a new camera capture
            * 
            * @param width The image width in pixels
            * @param height The image height in pixels
            * @param segments The segments of the image
            * @param depthImage The optional depth image of the scene
            * @param normalImage The optional normal image of the scene
            */
        public SDCNCameraCapture(
            int width,
            int height,
            SDCNSegment[] segments,
            byte[]? depthImage = null,
            byte[]? normalImage = null
        ) {
            Width = width;
            Height = height;
            Segments = segments;
            DepthImage = depthImage;
            NormalImage = normalImage;
        }
    };

    [Serializable]
    [Flags]
    public enum SDCNCameraMode {
        Segmented = 1,
        Depth = 2,
        Normal = 4
    };
}