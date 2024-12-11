using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIClickRedirect : MonoBehaviour, IPointerDownHandler
{
    [Header("Settings")]
    public UnityEvent OnClick;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnClick?.Invoke();
        Input.ResetInputAxes();
    }
}
