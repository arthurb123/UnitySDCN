#nullable enable

using System;
using System.Collections;
using UnityEngine;

namespace UnitySDCN {
    [ExecuteInEditMode]
    public class SDCNManager : MonoBehaviour
    {
        public static SDCNManager? Instance { get; private set; }

        public bool Rendering { get; private set; }
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

        private void Update()
        {
            // Check if the SDCNViewer is active,
            // if so we want to call it's update method
            if (_viewer != null)
                _viewer.Update();
        }

        public void RenderImage() {
            // Check if in play mode
            if (!Application.isPlaying) {
                SDCNLogger.Warning(
                    typeof(SDCNManager), 
                    "Could not render image, not in play mode"
                );
                return;
            }

            // Check if we are already rendering
            if (Rendering) {
                SDCNLogger.Warning(
                    typeof(SDCNManager), 
                    "Could not render image, already rendering"
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

            // Check if viewer is active, if so we need to switch back 
            // to the main camera
            IEnumerator hideViewer(Action callback) {
                if (_viewer != null && _viewer.IsShowingTexture) {
                    _viewer.SwitchBackToMainCamera();

                    // Wait for one frame
                    yield return null;
                }
                callback();
            }
            StartCoroutine(hideViewer(async () => {
                // Set rendering
                Rendering = true;

                // Capture camera data
                SDCNCameraCapture? cameraCapture = _camera.Capture();
                if (cameraCapture == null) {
                    SDCNLogger.Error(
                        typeof(SDCNManager), 
                        "Could not render image, failed to capture camera data"
                    );
                    return;
                }

                // Log
                SDCNLogger.Log(
                    typeof(SDCNManager), 
                    "Successfully captured camera data",
                    SDCNVerbosity.Minimal
                );

                // DEBUG: Save depth image to png file
                // if (cameraCapture.DepthImage != null)
                //     System.IO.File.WriteAllBytes("Assets/depth_image.png", cameraCapture.DepthImage);

                // DEBUG: Save normal image to png file
                // if (cameraCapture.NormalImage != null)
                //     System.IO.File.WriteAllBytes("Assets/normal_image.png", cameraCapture.NormalImage);

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

                // Reset rendering
                Rendering = false;
            }));
        }
    }
}