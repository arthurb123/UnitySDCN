#nullable enable

using UnityEngine;

namespace UnitySDCN {
    internal class SDCNViewer
    {
        private Camera? _mainCamera;
        private Camera? _textureCamera;
        private Texture2D? _overlayTexture;

        internal void ShowTexture(Texture2D overlayTexture, LayerMask excludeLayers)
        {
            // Step 0: Check if we are already showing a texture
            //         camera, if so, switch back to the main camera
            if (_textureCamera != null)
                SwitchBackToMainCamera();

            // Step 1: Store the overlay texture
            _overlayTexture = overlayTexture;

            // Step 2: Get reference to the main camera
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            // Step 3: Create a new Camera (we no longer need a RenderTexture)
            GameObject cameraObject = new("SDCNTextureCamera");
            _textureCamera = cameraObject.AddComponent<Camera>();

            // Set the new camera to match the main camera's position and orientation
            _textureCamera.transform.position = _mainCamera.transform.position;
            _textureCamera.transform.rotation = _mainCamera.transform.rotation;

            // Set the camera settings to match the main camera (optional: copy more settings if needed)
            _textureCamera.fieldOfView = _mainCamera.fieldOfView;
            _textureCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _textureCamera.farClipPlane = _mainCamera.farClipPlane;

            // Disable the new camera by default, we'll switch to it later
            _textureCamera.enabled = false;

            // Step 3: Exclude specific layers from the texture camera rendering
            _textureCamera.cullingMask = ~excludeLayers;

            // Step 4: Create a full-screen quad to show the Texture2D
            CreateFullscreenQuad();

            // Step 5: Switch from the main camera to the texture camera
            SwitchToTextureCamera();
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
            Material material = new(Shader.Find("Unlit/Transparent"))
            {
                mainTexture = _overlayTexture,
                renderQueue = 3000
            };
            quadRenderer.material = material;
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
            }
            if (_overlayTexture != null)
            {
                Object.Destroy(_overlayTexture);
                _overlayTexture = null;
            }
}
    }
}