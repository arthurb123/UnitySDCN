using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnitySDCN;

public class UIInteractiveManager : MonoBehaviour
{
    public static UIInteractiveManager Instance { get; private set; } = null;
    
    [Header("Scene")]
    public SDCNManager SDCNManager;
    public SDCNCamera SDCNCamera;
    [Space]
    public FreeCamera FreeCameraController;
    public RuntimeTransformHandle GizmoController;
    public PrimitiveSpawner PrimitiveSpawner;

    [Header("Components")]
    public GameObject RenderOverlayPanel;
    [Space]
    public GameObject ViewerPanel;
    public Slider ViewerTransparencySlider;
    [Space]
    public GameObject RenderPanel;
    [Space]
    public GameObject SpawnerPanel;
    [Space]
    public GameObject GuidePanel;
    [Space]
    public GameObject SelectedPanel;
    public Image SelectedPromptHighlightedImage;
    public Image SelectedPositionHighlightedImage;
    public Image SelectedRotationHighlightedImage;
    public Image SelectedScaleHighlightedImage;
    [Space]
    public GameObject PromptPanel;
    public TMP_InputField PromptTextField;
    public Slider PromptStrengthSlider;
    public TMP_InputField PromptStrengthInput;

    private PromptEditingMode _mode;
    private Vector3 _originalCameraPosition;
    private Quaternion _originalCameraRotation;

    public void SpawnCube() {
        PrimitiveSpawner.SpawnObject(PrimitiveType.Cube);
    }

    public void SpawnSphere() {
        PrimitiveSpawner.SpawnObject(PrimitiveType.Sphere);
    }

    public void SpawnCylinder() {
        PrimitiveSpawner.SpawnObject(PrimitiveType.Cylinder);
    }

    public void SpawnQuad() {
        PrimitiveSpawner.SpawnObject(PrimitiveType.Quad);
    }

    public void StartEditingObjectPrompt() {
        StartEditingPrompt(PromptEditingMode.Object);
    }

    public void StartEditingGlobalPrompt() {
        StartEditingPrompt(PromptEditingMode.Global);
    }

    private void StartEditingPrompt(PromptEditingMode mode) {
        // Set mode
        _mode = mode;

        // Set any editing prompt for editable SDCN objects
        UIEditableSDCNObject.AnyEditingPrompt = true;

        // Handle the prompt editing mode
        switch (mode)
        {
            case PromptEditingMode.Global:
                // Make sure the prompt text field is set to the global description
                PromptTextField.text = SDCNCamera.BackgroundDescription;
            break;
            case PromptEditingMode.Object:
                if (UIEditableSDCNObject.Selected != null) {
                    // Start editing prompt for the selected object
                    UIEditableSDCNObject.Selected.EditingPrompt = true;

                    // Make sure the prompt text field is set to the object's description
                    PromptTextField.text = UIEditableSDCNObject.Selected.SDCNObject.Description;
                }
                else
                    Debug.LogError("Attempted to edit object prompt without a selected object!");
            break;
        }

        // Show the prompt panel
        PromptPanel.SetActive(true);
    }

    public void StopEditingPrompt() {
        // Set any editing prompt for editable SDCN objects to false
        UIEditableSDCNObject.AnyEditingPrompt = false;

        // Handle the prompt editing mode
        switch (_mode)
        {
            case PromptEditingMode.Global:
                // Set the global description to the prompt text field
                SDCNCamera.BackgroundDescription = PromptTextField.text;
            break;
            case PromptEditingMode.Object:
                if (UIEditableSDCNObject.Selected != null) {
                    // Stop editing prompt for the selected object
                    UIEditableSDCNObject.Selected.EditingPrompt = false;

                    // Make sure the prompt text field is set to the object's description
                    UIEditableSDCNObject.Selected.SDCNObject.Description = PromptTextField.text;
                }
                else
                    Debug.LogError("Attempted to edit object prompt without a selected object!");
            break;
        }

        // Hide the prompt panel
        PromptPanel.SetActive(false);
    }

    public void ResetPromptStrength() {
        // Reset the prompt strength to 1.0
        if (UIEditableSDCNObject.Selected != null)
            PromptStrengthSlider.value = 1f;
    }

    public void ChangeSelectedHandleModeTranslation() {
        if (UIEditableSDCNObject.Selected != null)
            GizmoController.SetHandleMode(HandleType.POSITION);
    }

    public void ChangeSelectedHandleModeRotation() {
        if (UIEditableSDCNObject.Selected != null)
            GizmoController.SetHandleMode(HandleType.ROTATION);
    }

    public void ChangeSelectedHandleModeScale() {
        if (UIEditableSDCNObject.Selected != null)
            GizmoController.SetHandleMode(HandleType.SCALE);
    }

    public void DeselectSelectedObject() {
        if (UIEditableSDCNObject.Selected != null)
            UIEditableSDCNObject.Selected.Deselect();
    }

    public void DeleteSelectedObject() {
        if (UIEditableSDCNObject.Selected != null)
            UIEditableSDCNObject.Selected.Delete();
    }

    public void RenderImage() {
        // Disable the free camera controller
        FreeCameraController.enabled = false;

        // Enable the render overlay panel
        RenderOverlayPanel.SetActive(true);

        // Reset the viewer transparency slider in advance
        ViewerTransparencySlider.value = 1f;

        // For all SDCNObjects in the scene, we want to
        // temporarily change the layer from Outline
        // to Default if the object is selected
        UIEditableSDCNObject outlineObject = null;
        foreach (UIEditableSDCNObject obj in FindObjectsOfType<UIEditableSDCNObject>()) {
            if (obj.gameObject.layer == LayerMask.NameToLayer("Outline")) {
                obj.gameObject.layer = LayerMask.NameToLayer("Default");
                outlineObject = obj;
            }
        }

        // Issue a render call to the SDCNManager
        SDCNManager.RenderAndViewImage();

        // Change the layer back to Outline
        if (outlineObject != null)
            outlineObject.gameObject.layer = LayerMask.NameToLayer("Outline");
    }
    
    public void HideViewer() {
        // Hide the SDCNViewer
        SDCNViewer.Instance.Hide();

        // Enable the free camera controller
        FreeCameraController.enabled = true;

        // Hide the viewer panel
        ViewerPanel.SetActive(false);

        // Show the render panel
        RenderPanel.SetActive(true);

        // Show the spawner panel
        SpawnerPanel.SetActive(true);

        // Show the guide panel
        GuidePanel.SetActive(true);
    }

    public void UpdatePromptStrength() {
        if (UIEditableSDCNObject.Selected != null) {
            UIEditableSDCNObject.Selected.SDCNObject.Strength = 
                PromptStrengthInput.text == "" 
                    ? 0f 
                    : float.Parse(PromptStrengthInput.text);
            
            PromptStrengthSlider.value = UIEditableSDCNObject.Selected.SDCNObject.Strength;
        }
    }

    private void Start() {
        // Check instance, we only want one instance of this class
        if (Instance == null)
            Instance = this;
        else {
            Destroy(gameObject);
            return;
        }

        // Because we can setup rough maps outside of the interactive
        // demo, we want to update all materials of existing objects
        // once the interactive manager loads
        foreach (UIEditableSDCNObject obj in FindObjectsOfType<UIEditableSDCNObject>()) {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            PrimitiveSpawner.AssignColoredMaterial(renderer);
        }

        // Register original camera position and rotation
        _originalCameraPosition = SDCNCamera.transform.position;
        _originalCameraRotation = SDCNCamera.transform.rotation;
    }

    private void Update() {
        // If the gizmo controller is available and active,
        // show the selected panel and highlight the selected mode
        if (GizmoController != null 
        &&  GizmoController.gameObject.activeInHierarchy
        && !SDCNManager.Rendering) {
            SelectedPanel.SetActive(true);

            // Disable all highlights by default
            SelectedPromptHighlightedImage.enabled = false;
            SelectedPositionHighlightedImage.enabled = false;
            SelectedRotationHighlightedImage.enabled = false;
            SelectedScaleHighlightedImage.enabled = false;

            // Check if editing prompt
            if (UIEditableSDCNObject.Selected != null
            &&  UIEditableSDCNObject.Selected.EditingPrompt) {
                // If the prompt panel is not yet visible,
                // we want to set the text field to the object's description
                if (!PromptPanel.activeInHierarchy) {
                    StartEditingObjectPrompt();

                    // Highlight the selected prompt
                    SelectedPromptHighlightedImage.enabled = true;

                    // Disable the free camera controller
                    FreeCameraController.enabled = false;

                    // Setup the slider
                    PromptStrengthSlider.maxValue = 10f;
                    PromptStrengthSlider.minValue = 0f;
                    PromptStrengthSlider.value = UIEditableSDCNObject.Selected.SDCNObject.Strength;
                    void setStrength(float value) {
                        UIEditableSDCNObject.Selected.SDCNObject.Strength = value;
                        if (!PromptStrengthInput.isFocused)
                            PromptStrengthInput.text = $"{value.ToString("0.00")}";
                    }

                    PromptStrengthSlider.onValueChanged.RemoveAllListeners();
                    PromptStrengthSlider.onValueChanged.AddListener(setStrength);
                    setStrength(UIEditableSDCNObject.Selected.SDCNObject.Strength);
                }
            }

            // Otherwise, check the gizmo controller mode
            else {
                // Hide the prompt panel
                PromptPanel.SetActive(false);

                // Enable the free camera controller
                FreeCameraController.enabled = true;

                // Highlight the selected mode
                switch (GizmoController.type) {
                    case HandleType.POSITION:
                        SelectedPositionHighlightedImage.enabled = true;
                        break;
                    case HandleType.ROTATION:
                        SelectedRotationHighlightedImage.enabled = true;
                        break;
                    case HandleType.SCALE:
                        SelectedScaleHighlightedImage.enabled = true;
                        break;
                }
            }
        }
        else
            SelectedPanel.SetActive(false);

        // Check if we the prompt panel is active, we
        // do not want to update the scene while editing
        if (PromptPanel.activeInHierarchy)
            return;

        // Check if user pressed the space button, if we are
        // not already rendering we want to start rendering
        if (Input.GetKeyDown(KeyCode.Space) 
        && !RenderOverlayPanel.activeInHierarchy
        && !SDCNViewer.Active) {
            RenderImage();
            return;
        }

        // Check if the user pressed the tab button, if we
        // are not rendering and not viewing we want to reset
        // the camera position to the original position
        if (Input.GetKeyDown(KeyCode.Tab)
        && !RenderOverlayPanel.activeInHierarchy
        && !SDCNViewer.Active) {
            SDCNCamera.transform.position = _originalCameraPosition;
            SDCNCamera.transform.rotation = _originalCameraRotation;
            return;
        }

        // If the render overlay is active, and the SDCNManager is not
        // rendering, we want to disable the render overlay panel. We do
        // not want to re-enable the free camera, as a texture viewer is
        // shown after rendering which will block the camera view
        if (RenderOverlayPanel.activeInHierarchy && !SDCNManager.Rendering) {
            // Disable the render overlay panel
            RenderOverlayPanel.SetActive(false);
        }

        // If the user pressed the escape button while the SDCNViewer is active,
        // we want to hide the viewer and enable the free camera controller
        if (SDCNViewer.Active) {
            // Show viewer panel
            ViewerPanel.SetActive(true);

            // Hide the render panel
            RenderPanel.SetActive(false);

            // Hide the spawner panel
            SpawnerPanel.SetActive(false);

            // Hide the guide panel
            GuidePanel.SetActive(false);

            // Set the viewer opacity
            SDCNViewer.Instance.Opacity = ViewerTransparencySlider.value;

            if (Input.GetKeyDown(KeyCode.Escape))
                HideViewer();
        }
    }

    private enum PromptEditingMode {
        Global,
        Object
    };
}