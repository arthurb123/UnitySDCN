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

            // Capture camera data
            SDCNCameraCapture? cameraCapture = _camera.Capture();
            if (cameraCapture == null) {
                SDCNLogger.Error(
                    typeof(SDCNManager), 
                    "Could not render image, failed to capture camera data"
                );
                return;
            }

            // DEBUG: Save depth image to png file
            // if (cameraCapture.DepthImage != null)
            //     System.IO.File.WriteAllBytes("Assets/depth_image.png", cameraCapture.DepthImage);

            // DEBUG: Save normal image to png file
            if (cameraCapture.NormalImage != null)
                System.IO.File.WriteAllBytes("Assets/normal_image.png", cameraCapture.NormalImage);

            // DEBUG: Save segmented image to png file
            // for (int i = 0; i < cameraCapture.Segments.Length; i++) {
            //     string path = "Assets/segmented_image_" + i + ".png";
            //     System.IO.File.WriteAllBytes(path, cameraCapture.Segments[i].MaskImage);
            // }

            // Generate image through web client using
            // the captured camera data
            Texture2D? texture = await SDCNWebClient.GenerateImage(
                serverAddress: _webServerAddress,
                width: cameraCapture.Width,
                height: cameraCapture.Height,
                segments: cameraCapture.Segments,
                depthImage: cameraCapture.DepthImage,
                normalImage: cameraCapture.NormalImage,
                backgroundDescription: _camera.BackgroundDescription,
                negativeDescription: _camera.NegativeDescription
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