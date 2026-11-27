using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GenericButton2D : MonoBehaviour,IPointerEnterHandler,IPointerClickHandler,IPointerExitHandler
{
    private Action buttonAction;
    public void SetupButton(Action ClickButtonAction = null)
    {
        buttonAction = ClickButtonAction;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        buttonAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
}
