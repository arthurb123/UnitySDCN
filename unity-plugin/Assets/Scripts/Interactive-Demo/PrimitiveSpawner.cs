using UnityEngine;
using System.Collections;
using UnitySDCN;
using UnityEngine.Rendering;
using RuntimeHandle;

public class PrimitiveSpawner : MonoBehaviour {
    [Header("Scene")]
    public SDCNManager SDCNManager;
    public FreeCamera FreeCameraController;
    public Transform ObjectContainer;

    [Header("Material Settings")]
    public Material BaseMaterial;

    public void SpawnObject(PrimitiveType primitiveType) {
        // Check if the SDCNViewer is active, or if we are rendering
        if (SDCNViewer.Active || SDCNManager.Rendering)
            return;

        // Spawn a new primitive
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.transform.position = FreeCameraController.transform.position + FreeCameraController.transform.forward * 5f;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.transform.SetParent(ObjectContainer);

        // Add a editable SDCN object component
        UIEditableSDCNObject editableObj = obj.AddComponent<UIEditableSDCNObject>();

        // Assign the new material to the primitive's renderer
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        AssignColoredMaterial(renderer);

        // Wait for one frame, then select the object
        IEnumerator selectObject() {
            yield return null;
            editableObj.Select();
        }
        StartCoroutine(selectObject());
    }

    public void AssignColoredMaterial(MeshRenderer renderer) {
        // Create a new material instance based on the base material
        Material newMat = new Material(BaseMaterial);

        // Generate a random base color of the material such that
        // objects are not all the same color, this is especially
        // useful when multiple objects are overlapping and we are
        // dealing with poor lighting conditions
        Color randomColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        newMat.SetColor("_BaseColor", randomColor);

        // Assign the new material to the primitive's renderer
        renderer.material = newMat;
    }
}
