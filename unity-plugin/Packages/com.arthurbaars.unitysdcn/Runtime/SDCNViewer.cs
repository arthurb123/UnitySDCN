#nullable enable

using UnityEngine;

namespace UnitySDCN {
    public class SDCNViewer
    {
        public static SDCNViewer? Instance { get; private set; }
        public static bool Active => Instance != null && Instance.IsShowingTexture;

        public float Opacity {
            get => _overlayOpacity;
            set {
                _overlayOpacity = Mathf.Clamp01(value);
            }
        }
        internal bool IsShowingTexture => _textureCamera != null;

        private Camera? _mainCamera;
        private Camera? _textureCamera;
        private Texture2D? _overlayTexture;
        private Material? _overlayMaterial;
        private float _overlayOpacity = 1.0f;

        public void Hide() {
            SwitchBackToMainCamera();
        }

        internal void Update() {
            if (_overlayMaterial != null)
                _overlayMaterial.color = new Color(1, 1, 1, _overlayOpacity);
        }

        internal void ShowTexture(Texture2D overlayTexture, LayerMask excludeLayers)
        {
            // Step 0: Check if we are already showing a texture
            //         camera, if so, switch back to the main camera
            if (_textureCamera != null)
                SwitchBackToMainCamera();

            // Step 1: Store the overlay texture
            _overlayTexture = overlayTexture;

            // Step 2: Get reference to the main camera
            //         and make sure it's depth is -1
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            _mainCamera.depth = -1;

            // Step 3: Create a new camera
            GameObject cameraObject = new("SDCNTextureCamera");
            _textureCamera = cameraObject.AddComponent<Camera>();
            _textureCamera.depth = 0;

            // Set the new camera to match the main camera's position and orientation
            _textureCamera.transform.position = _mainCamera.transform.position;
            _textureCamera.transform.rotation = _mainCamera.transform.rotation;

            // Set the camera settings to match the main camera
            _textureCamera.fieldOfView = _mainCamera.fieldOfView;
            _textureCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _textureCamera.farClipPlane = _mainCamera.farClipPlane;
            _textureCamera.clearFlags = CameraClearFlags.Depth; // Avoid overwriting the main camera's rendering

            // Disable the new camera by default, we'll switch to it later
            _textureCamera.enabled = false;

            // Step 3: Exclude specific layers from the texture camera rendering
            _textureCamera.cullingMask = ~excludeLayers;

            // Step 4: Create a full-screen quad to show the Texture2D
            CreateFullscreenQuad();

            // Step 5: Switch from the main camera to the texture camera
            SwitchToTextureCamera();

            // Set instance
            Instance = this;
        }

        internal void CreateFullscreenQuad()
        {
            // Check if texture camera is available
            if (_textureCamera == null || _overlayTexture == null)
                return;

            // Create a quad object in front of the camera to show the texture
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(_textureCamera.transform);

            // Move the quad slightly in front of the camera
            float distanceFromCamera = 0.65f;
            quad.transform.localPosition = new Vector3(0, 0, distanceFromCamera); 

            // The quad needs to fill the entire camera view
            float height = 2.0f * distanceFromCamera * Mathf.Tan(_textureCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * _textureCamera.aspect;
            quad.transform.localScale = new Vector3(width, height, 1);

            // Reset quad rotation
            quad.transform.localRotation = Quaternion.identity;

            // Assign the provided Texture2D as the quad's material texture
            Renderer quadRenderer = quad.GetComponent<Renderer>();
           _overlayMaterial = new Material(Shader.Find("SDCN/UnlitTransparent"))
            {
                mainTexture = _overlayTexture,
                renderQueue = 3000
            };
            _overlayMaterial.color = new Color(1, 1, 1, _overlayOpacity);
            quadRenderer.material = _overlayMaterial;
        }

        internal void SwitchToTextureCamera()
        {
            // Check if cameras are available
            if (_mainCamera == null || _textureCamera == null)
                return;

            // Disable the main camera and enable the texture camera
            _textureCamera.enabled = true;
        }

        internal void SwitchBackToMainCamera()
        {
            // Check if cameras are available
            if (_mainCamera == null || _textureCamera == null)
                return;

            // Destroy the dynamically created texture camera
            if (_textureCamera != null)
            {
                Object.Destroy(_textureCamera.gameObject);
                _textureCamera = null;
                Instance = null;
            }
            if (_overlayTexture != null)
            {
                Object.Destroy(_overlayTexture);
                _overlayTexture = null;
            }
        }
    }
}