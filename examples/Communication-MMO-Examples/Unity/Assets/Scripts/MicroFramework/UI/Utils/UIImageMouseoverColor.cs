using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIImageMouseoverColor : BehaviourNonAlloc, IPointerEnterHandler, IPointerExitHandler
{
    public Image image;
    public Color highlightColor = Color.white;
    Color defaultColor;

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        defaultColor = image.color;
        image.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        image.color = defaultColor;
    }
}