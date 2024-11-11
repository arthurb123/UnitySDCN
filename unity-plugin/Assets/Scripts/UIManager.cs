using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
    [Header("Scene")]
    public FreeCamera FreeCameraController;
    public RuntimeTransformHandle GizmoController;

    [Header("Components")]
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
        if (GizmoController != null && GizmoController.gameObject.activeInHierarchy) {
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
