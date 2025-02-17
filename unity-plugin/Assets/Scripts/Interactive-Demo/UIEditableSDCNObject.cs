using System;
using UnityEngine;
using UnitySDCN;
using RuntimeHandle;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SDCNObject))]
public class UIEditableSDCNObject : MonoBehaviour
{
    public static UIEditableSDCNObject Selected { get; private set; } = null;
    public static bool AnyEditingPrompt { get; set; } = false;
    
    public bool EditingPrompt { get; set; }
    public SDCNObject SDCNObject { get; private set; }

    private LayerMask _uiLayer;
    private bool _isMouseOver;
    private RuntimeTransformHandle _gizmoController;
    
    void Start()
    {
        // Sanity checks
        SDCNObject = GetComponent<SDCNObject>();
        if (SDCNObject == null)
            throw new Exception("Could not find required SDCNObject on GameObject!");

        // Find RuntimeTransformHandle in scene
        _gizmoController = FindObjectOfType<RuntimeTransformHandle>(true);
        if (_gizmoController == null)
            throw new Exception("Could not find RuntimeTransformHandle in scene!");

        // Set UI layer
        _uiLayer = LayerMask.NameToLayer("UI");
    }

    // We run this in late update so that the gizmo
    // controller has priority over handling input events
    void LateUpdate() {
        // If the SDCNViewer is active, we
        // are rendering, or we are editing
        // ANY prompt - we should not be able to
        // interact with the scene
        if (SDCNViewer.Active
        ||  SDCNManager.Instance.Rendering
        ||  AnyEditingPrompt) {
            if (!EditingPrompt && Selected == this)
                Deselect();
            return;
        }

        // Get mouse over UI state
        bool isMouseOverUI = IsPointerOverUIElement(GetEventSystemRaycastResults());
        bool isMouseOverGizmo = _gizmoController.DraggingHandle;
        if (isMouseOverUI) {
            // Reset outline and tooltip if mouse
            // is currently over the game object
            if (_isMouseOver) {
                _isMouseOver = false;
                UITooltip.Instance.Hide();
            }
        }

        // Check if mouse was pressed down (left click)
        if (Input.GetMouseButtonDown(0)) {
            // Check if the mouse is over UI, in this case
            // we do not want to interact with the scene.
            // We differentiate UI using the tag "UI"
            if (!isMouseOverUI && !isMouseOverGizmo) {
                // Check if mouse is over this object
                if (Selected == null && _isMouseOver)
                    Select();

                // If the mouse is not over this object,
                // but it is selected we want to deselect
                else if (Selected == this && !_isMouseOver)
                    Deselect();
            }
        }

        // Check if this object is selected
        if (Selected == this) {
            // Handle key down
            if (Input.GetKeyDown(KeyCode.E))
                EditPrompt();
            else if (!EditingPrompt) {
                if (Input.GetKeyDown(KeyCode.T))
                    SetHandleModeTranslation();
                else if (Input.GetKeyDown(KeyCode.R))
                    SetHandleModeRotation();
                else if (Input.GetKeyDown(KeyCode.F))
                    SetHandleModeScale();
                else if (Input.GetKeyDown(KeyCode.Escape))
                    Deselect();
                else if (Input.GetKeyDown(KeyCode.X)) {
                    Delete();
                    return;
                }
            }
        }

        // Handle outline
        if (_isMouseOver && Selected == null || Selected == this)
            transform.gameObject.layer = LayerMask.NameToLayer("Outline");
        else 
            transform.gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void EditPrompt() {
        if (Selected == this)
            EditingPrompt = true;
    }

    public void SetHandleModeTranslation() {
        if (Selected == this)
            _gizmoController.SetHandleMode(HandleType.POSITION);
    }

    public void SetHandleModeRotation() {
        if (Selected == this)
            _gizmoController.SetHandleMode(HandleType.ROTATION);
    }

    public void SetHandleModeScale() {
        if (Selected == this)
            _gizmoController.SetHandleMode(HandleType.SCALE);
    }

    public void Delete() {
        if (Selected == this) {
            Deselect();
            Destroy(gameObject);
        }
    }

    public void Select() {
        // Check if already selected
        if (Selected == this)
            return;

        // Set gizmo target
        _gizmoController.SetTarget(transform);
        _gizmoController.gameObject.SetActive(true);

        // Set as selected
        Selected = this;
    }

    public void Deselect() {
        // Check if this object is not selected
        if (Selected != this)
            return;

        // Reset gizmo target
        _gizmoController.target = null;
        _gizmoController.gameObject.SetActive(false);

        // Reset selected object, if it's this object
        if (Selected == this)
            Selected = null;
    }

    private void OnMouseOver() {
        // Set flag
        _isMouseOver = true;

        // Show tooltip, but only if not editing,
        // not rendering, we are not actively showing
        // a SDCNViewer and the object selection is valid
        if (!EditingPrompt
        &&  !SDCNManager.Instance.Rendering
        &&  !SDCNViewer.Active
        && (Selected == null || Selected == this)) {
            string description = SDCNObject.Description;
            if (description == null || description == string.Empty)
                description = "(No description yet set)";
            string fullDescription = $"{description}\n\nStrength: {SDCNObject.Strength.ToString("0.00")}";
            UITooltip.Instance.Show(gameObject, fullDescription);
        }
    }

    private void OnMouseExit() {
        // Reset flag
        _isMouseOver = false;

        // Hide tooltip, if we are the owner
        if (UITooltip.Instance.Owner == gameObject
        ||  SDCNManager.Instance.Rendering
        ||  SDCNViewer.Active)
            UITooltip.Instance.Hide();
    }

    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == _uiLayer)
                return true;
        }
        return false;
    }

    private List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
