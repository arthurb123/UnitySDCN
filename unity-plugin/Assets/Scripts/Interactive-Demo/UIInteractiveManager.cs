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
    [Header("Scene")]
    public SDCNManager SDCNManager;
    public FreeCamera FreeCameraController;
    public RuntimeTransformHandle GizmoController;
    public Transform ObjectContainer;

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
    public GameObject SelectedPanel;
    public Image SelectedPromptHighlightedImage;
    public Image SelectedPositionHighlightedImage;
    public Image SelectedRotationHighlightedImage;
    public Image SelectedScaleHighlightedImage;
    [Space]
    public GameObject PromptPanel;
    public TMP_InputField PromptTextField;
    public Slider PromptStrengthSlider;
    public TMP_Text PromptStrengthText;

    void Update() {
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
                SelectedPromptHighlightedImage.enabled = true;

                // If the prompt panel is not yet visible,
                // we want to set the text field to the object's description
                if (!PromptPanel.activeInHierarchy)
                    PromptTextField.text = UIEditableSDCNObject.Selected.SDCNObject.Description;

                // Show the prompt panel
                PromptPanel.SetActive(true);

                // Disable the free camera controller
                FreeCameraController.enabled = false;

                // Setup the slider
                PromptStrengthSlider.maxValue = 4f;
                PromptStrengthSlider.minValue = 0f;
                PromptStrengthSlider.value = UIEditableSDCNObject.Selected.SDCNObject.Strength;
                void setStrength(float value) {
                    UIEditableSDCNObject.Selected.SDCNObject.Strength = value;
                    PromptStrengthText.text = $"Strength: {value}";
                }

                PromptStrengthSlider.onValueChanged.RemoveAllListeners();
                PromptStrengthSlider.onValueChanged.AddListener(setStrength);
                setStrength(UIEditableSDCNObject.Selected.SDCNObject.Strength);
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
        // do not want to render the scene while editing
        if (PromptPanel.activeInHierarchy)
            return;

        // Check if user pressed the space button, if we are
        // not already rendering we want to start rendering
        if (Input.GetKeyDown(KeyCode.Space) 
        && !RenderOverlayPanel.activeInHierarchy
        && !SDCNViewer.Active) {
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

            // Set the viewer opacity
            SDCNViewer.Instance.Opacity = ViewerTransparencySlider.value;

            if (Input.GetKeyDown(KeyCode.Escape)) {
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
            }
        }
    }

    public void StopEditingPrompt() {
        // Stop editing prompt for selected object
        if (UIEditableSDCNObject.Selected != null) {
            // Set the object's description to the prompt text field
            UIEditableSDCNObject.Selected.SDCNObject.Description = PromptTextField.text;

            // Stop editing prompt
            UIEditableSDCNObject.Selected.EditingPrompt = false;
        }
    }

    public void ResetPromptStrength() {
        // Reset the prompt strength to 1.0
        if (UIEditableSDCNObject.Selected != null)
            PromptStrengthSlider.value = 1f;
    }

    public void SpawnCube() {
        SpawnObject(PrimitiveType.Cube);
    }

    public void SpawnSphere() {
        SpawnObject(PrimitiveType.Sphere);
    }

    public void SpawnCapsule() {
        SpawnObject(PrimitiveType.Capsule);
    }

    public void SpawnCylinder() {
        SpawnObject(PrimitiveType.Cylinder);
    }

    public void SpawnPlane() {
        SpawnObject(PrimitiveType.Plane);
    }

    public void SpawnQuad() {
        SpawnObject(PrimitiveType.Quad);
    }

    private void SpawnObject(PrimitiveType primitiveType) {
        // Check if the SDCNViewer is active, or we
        // are rendering, we should not be able to
        // interact with the scene
        if (SDCNViewer.Active
        ||  SDCNManager.Rendering)
            return;

        // Spawn a new object
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.transform.position = FreeCameraController.transform.position + FreeCameraController.transform.forward * 5f;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(ObjectContainer);

        // Add the UIEditableSDCNObject component
        UIEditableSDCNObject editableObj = obj.AddComponent<UIEditableSDCNObject>();

        // Wait for one frame, then select the object
        IEnumerator selectObject() {
            yield return null;
            editableObj.Select();
        }
        StartCoroutine(selectObject());
    }
}
