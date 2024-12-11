using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class UITooltip : MonoBehaviour
{
    private readonly Vector2 TOOLTIP_OFFSET = new Vector2(24, -16);

    public static UITooltip Instance { get; private set; } = null;
    public GameObject Owner { get; private set; } = null;

    [Header("Tooltip Components")]
    public TextMeshProUGUI DescriptionText;

    public void Show(GameObject owner, string description) {
        Owner = owner;
        DescriptionText.text = description;
        gameObject.SetActive(true);
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    private void Start() {
        if (Instance == null) {
            Instance = this;
            Hide();
        }
        else
            Destroy(gameObject);
    }

    private void Update() {
        // Update position
        transform.position = new Vector3(
            Input.mousePosition.x + TOOLTIP_OFFSET.x,
            Input.mousePosition.y + TOOLTIP_OFFSET.y,
            0
        );

        // Check if the mouse is over any
        // other gameobjects on the UI layer
        // except this one, we then want
        // to hide the tooltip.
        if (IsPointerOverUI())
            Hide();
    }

    private bool IsPointerOverUI()
    {
        // Create a PointerEventData for the current EventSystem
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        // Perform a raycast using the current EventSystem
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        // Return true if any GameObject is detected
        return raycastResults.Count > 0;
    }
}
