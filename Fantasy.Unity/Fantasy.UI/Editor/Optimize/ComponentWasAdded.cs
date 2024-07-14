using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fantasy
{
    [InitializeOnLoad]
    public class ComponentWasAdded
    {
        static ComponentWasAdded()
        {
            ObjectFactory.componentWasAdded += component =>
            {
                if (component is MaskableGraphic graphic)
                {
                    if (graphic.GetComponent<Selectable>() == null)
                        graphic.raycastTarget = false;
                }

                if (component is Selectable
                    or IBeginDragHandler
                    or IEndDragHandler
                    or IDragHandler
                    or IPointerEnterHandler
                    or IPointerExitHandler
                    or IPointerDownHandler
                    or IPointerUpHandler
                    or IPointerClickHandler)
                {
                    var maskableGraphic = component.GetComponent<MaskableGraphic>();
                    if (maskableGraphic != null)
                        maskableGraphic.raycastTarget = true;
                }
            };
        }
    }
}