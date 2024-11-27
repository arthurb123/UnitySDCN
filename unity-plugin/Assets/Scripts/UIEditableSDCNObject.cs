using System;
using UnityEngine;
using UnitySDCN;
using RuntimeHandle;

[RequireComponent(typeof(SDCNObject))]
public class UIEditableSDCNObject : MonoBehaviour
{
    public static UIEditableSDCNObject Selected { get; private set; }
    
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
        // If the SDCNViewer is active, or we
        // are rendering, we should not be able to
        // interact with the scene
        if (SDCNViewer.Active
        ||  SDCNManager.Instance.Rendering) {
            if (Selected == this)
                Deselect();
            return;
        }

        // Check if mouse was pressed down (left click)
        if (Selected == null && Input.GetMouseButtonDown(0)) {
            // Check if mouse is over this object
            if (_isMouseOver) {
                // Deselect previous object
                if (Selected != null)
                    Selected.Deselect();

                // Select this object
                Select();
            }
        }

        // Check if this object is selected
        if (Selected == this) {
            // Handle key down
            if (Input.GetKeyDown(KeyCode.E))
                EditingPrompt = true;
            else if (!EditingPrompt) {
                if (Input.GetKeyDown(KeyCode.T))
                    _gizmoController.SetHandleMode(HandleType.POSITION);
                else if (Input.GetKeyDown(KeyCode.R))
                    _gizmoController.SetHandleMode(HandleType.ROTATION);
                else if (Input.GetKeyDown(KeyCode.F))
                    _gizmoController.SetHandleMode(HandleType.SCALE);
                else if (Input.GetKeyDown(KeyCode.Escape))
                    Deselect();
                else if (Input.GetKeyDown(KeyCode.X)) {
                    Deselect();
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // Handle outline
        if (_isMouseOver && Selected == null)
            transform.gameObject.layer = LayerMask.NameToLayer("Outline");
        else 
            transform.gameObject.layer = LayerMask.NameToLayer("Default");
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
    }

    void OnMouseExit() {
        // Reset flag
        _isMouseOver = false;
    }
}
