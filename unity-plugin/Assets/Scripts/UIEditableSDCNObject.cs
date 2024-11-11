using System;
using UnityEngine;
using UnitySDCN;

[RequireComponent(typeof(SDCNObject))]
public class UIEditableSDCNObject : MonoBehaviour
{
    private SDCNObject _sdcnObject;
    
    void Start()
    {
        if (!TryGetComponent(out _sdcnObject))
            throw new Exception("Could not find required SDCNObject on GameObject!");
    }

    void OnMouseOver() {
        Debug.Log("Looking at " + _sdcnObject.Description);
    }

    void OnMouseExit() {
        Debug.Log("Left");
    }
}
