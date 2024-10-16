#nullable enable

using System;
using UnityEngine;

namespace UnitySDCN {
    [ExecuteInEditMode]
    public class SDCNManager : MonoBehaviour
    {
        public static SDCNManager? Instance { get; private set; }

        public SDCNVerbosity LogVerbosity => _logVerbosity;
        public string WebServerAddress => _webServerAddress;

        [Header("SDCN Components")]
        [SerializeField] private SDCNCamera? _camera = null;

        [Header("Settings")]
        [SerializeField] private string _webServerAddress = "http://127.0.0.1:9295";
        [SerializeField] private SDCNVerbosity _logVerbosity = SDCNVerbosity.Minimal;

        private SDCNViewer? _viewer;

        private void Awake()
        {
            // Attempt to set the instance of SDCNManager
            if (Instance != null && Instance != this)
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "An instance of SDCNManager already exists in the scene"
                );
            else
                Instance = this;
        }

        public async void RenderImage() {
            // Check if in play mode
            if (!Application.isPlaying) {
                SDCNLogger.Warning(
                    typeof(SDCNManager), 
                    "Could not render image, not in play mode"
                );
                return;
            }

            // Check if have a valid camera
            if (_camera == null) {
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "Could not render image, no camera found in SDCNManager"
                );
                return;
            }

            // Render depth image from camera
            byte[]? depthImage = _camera.CaptureDepthImage();
            if (depthImage == null) {
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "Could not render image, failed to capture depth image"
                );
                return;
            }

            // Capture segmented image from camera
            SDCNSegment[]? segments = _camera.CaptureSegmentedImage();
            if (segments == null) {
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "Could not render image, failed to capture segmented image"
                );
                return;
            }

            // DEBUG: Save depth image to png file
            // System.IO.File.WriteAllBytes("Assets/depth_image.png", depthImage);

            // DEBUG: Save segmented image to png file
            // for (int i = 0; i < segments.Length; i++) {
            //     string path = "Assets/segmented_image_" + i + ".png";
            //     System.IO.File.WriteAllBytes(path, segments[i].MaskImage);
            // }

            // Generate image from segments
            Texture2D? texture = await SDCNWebClient.GenerateImage(
                depthImage,
                segments,
                _camera.BackgroundDescription,
                _camera.NegativeDescription
            );

            // Check if texture is valid
            if (texture == null) {
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "Received invalid texture from server"
                );
                return;
            }

            // Check if there is currently a viewer active
            if (_viewer == null)
                _viewer = new SDCNViewer();
            else
                _viewer.SwitchBackToMainCamera();

            // View texture
            _viewer.ShowTexture(texture, _camera.ExcludeLayers);

            // Log
            SDCNLogger.Log(
                typeof(SDCNManager), 
                "Successfully rendered image, created viewer to display texture"
            );
        }
    }
}