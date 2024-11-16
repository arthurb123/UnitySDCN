using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnitySDCN;

public class UIManager : MonoBehaviour
{
    [Header("Scene")]
    public SDCNManager SDCNManager;
    public FreeCamera FreeCameraController;
    public RuntimeTransformHandle GizmoController;

    [Header("Components")]
    public GameObject RenderOverlayPanel;
    [Space]
    public GameObject HideViewerPanel;
    public GameObject RenderPanel;
    [Space]
    public GameObject SelectedPanel;
    public Image SelectedPromptHighlightedImage;
    public Image SelectedPositionHighlightedImage;
    public Image SelectedRotationHighlightedImage;
    public Image SelectedScaleHighlightedImage;
    [Space]
    public GameObject PromptPanel;
    public TMP_InputField PromptTextField;

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

            // Issue a render call to the SDCNManager
            SDCNManager.RenderImage();
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
            // Show hide viewer panel
            HideViewerPanel.SetActive(true);

            // Hide the render panel
            RenderPanel.SetActive(false);

            if (Input.GetKeyDown(KeyCode.Escape)) {
                // Hide the SDCNViewer
                SDCNViewer.Instance.Hide();

                // Enable the free camera controller
                FreeCameraController.enabled = true;

                // Hide the hide viewer panel
                HideViewerPanel.SetActive(false);

                // Show the render
                RenderPanel.SetActive(true);
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
}
