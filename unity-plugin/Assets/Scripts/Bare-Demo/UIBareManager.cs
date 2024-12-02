using System.Collections;
using System.Collections.Generic;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnitySDCN;

public class UIBareManager : MonoBehaviour
{
    [Header("Scene")]
    public SDCNManager SDCNManager;
    public SDCNObject MainQuad;

    [Header("Components")]
    public GameObject RenderOverlayPanel;
    [Space]
    public GameObject ViewerPanel;
    [Space]
    public GameObject PromptPanel;
    public TMP_InputField PromptTextField;

    void Update() {
        // If the user pressed the escape button while the SDCNViewer is active,
        // we want to hide the viewer and resume the prompt UI
        if (SDCNViewer.Active) {
            // Hide the render overlay
            RenderOverlayPanel.SetActive(false);

            // Show viewer panel
            ViewerPanel.SetActive(true);

            // Hide the prompt panel
            PromptPanel.SetActive(false);

            if (Input.GetKeyDown(KeyCode.Escape)) {
                // Hide the SDCNViewer
                SDCNViewer.Instance.Hide();

                // Hide the viewer panel
                ViewerPanel.SetActive(false);

                // Show the prompt panel
                PromptPanel.SetActive(true);
            }
        }
    }

    public void Render() {
        // Check if we are not already rendering
        if (!SDCNManager.Rendering) {
            // First, set the prompt of the main quad to the text field value
            MainQuad.Description = PromptTextField.text;
            MainQuad.Strength = 1.0f;

            // Enable the render overlay panel
            RenderOverlayPanel.SetActive(true);

            // Hide the prompt panel
            PromptPanel.SetActive(false);

            // Issue a render call to the SDCNManager
            SDCNManager.RenderAndViewImage();
        }
    }
}
