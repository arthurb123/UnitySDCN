using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        transform.position = new Vector3(
            Input.mousePosition.x + TOOLTIP_OFFSET.x,
            Input.mousePosition.y + TOOLTIP_OFFSET.y,
            0
        );
    }
}
