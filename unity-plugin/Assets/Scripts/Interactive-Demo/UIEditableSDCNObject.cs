using System;
using UnityEngine;
using UnitySDCN;
using RuntimeHandle;

[RequireComponent(typeof(SDCNObject))]
public class UIEditableSDCNObject : MonoBehaviour
{
    public static UIEditableSDCNObject Selected { get; private set; } = null;
    
    public bool EditingPrompt { get; set; }
    public SDCNObject SDCNObject { get; private set; }

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
    }

    void Update() {
        // If the SDCNViewer is active, we
        // are rendering, or we are editing
        // the prompt - we should not be able to
        // interact with the scene
        if (SDCNViewer.Active
        ||  SDCNManager.Instance.Rendering
        ||  EditingPrompt) {
            if (Selected == this)
                Deselect();
            return;
        }

        // Check if mouse was pressed down (left click)
        if (Input.GetMouseButtonDown(0)) {
            // Check if mouse is over this object
            if (Selected == null && _isMouseOver)
                Select();

            // If the mouse is not over this object,
            // but it is selected we want to deselect
            else if (Selected == this && !_isMouseOver)
                Deselect();
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

    void OnMouseOver() {
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

    void OnMouseExit() {
        // Reset flag
        _isMouseOver = false;

        // Hide tooltip, if we are the owner
        if (UITooltip.Instance.Owner == gameObject
        ||  SDCNManager.Instance.Rendering
        ||  SDCNViewer.Active)
            UITooltip.Instance.Hide();
    }
}
