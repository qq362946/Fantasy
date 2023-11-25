using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// UI拖放组件

public class UIDragAndDropable : BehaviourNonAlloc , IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // drag options
    public PointerEventData.InputButton button = PointerEventData.InputButton.Left;
    public GameObject drageePrefab;
    public static GameObject currentlyDragged;

    // 拖放状态
    public bool dragable = true;
    public bool dropable = true;

    [HideInInspector] public bool draggedToSlot = false;
    public int itemCat;

    public void OnBeginDrag(PointerEventData d)
    {
        if (dragable && d.button == button)
        {
            // load current
            currentlyDragged = Instantiate(drageePrefab, transform.position, Quaternion.identity);
            currentlyDragged.GetComponent<Image>().sprite = GetComponent<Image>().sprite;
            currentlyDragged.GetComponent<Image>().color = GetComponent<Image>().color; // for durability etc.
            currentlyDragged.transform.SetParent(transform.root, true); 
            currentlyDragged.transform.SetAsLastSibling(); // 移到前景

            //拖动时禁用按钮，这样被拖动的格子道具的onClick不会被触发
            GetComponent<Button>().interactable = false;
        }
    }

    public void OnDrag(PointerEventData d)
    {
        if (dragable && d.button == button)
            currentlyDragged.transform.position = d.position;
    }

    // 在格子的OnDrop之后调用
    public void OnEndDrag(PointerEventData d)
    {
        Destroy(currentlyDragged);

        if (dragable && d.button == button)
        {
            if (!draggedToSlot && d.pointerEnter == null)
            {
                SendMessage("OnDragAndClear_" + tag,name.ToInt(),
                    SendMessageOptions.DontRequireReceiver);
            }

            // reset 
            draggedToSlot = false;

            GetComponent<Button>().interactable = true;
        }
    }

    // d.pointerDrag 是被拖动的对象
    public void OnDrop(PointerEventData d)
    {
        if (dropable && d.button == button)
        {
            UIDragAndDropable dropDragable = d.pointerDrag.GetComponent<UIDragAndDropable>();
            if (dropDragable != null && dropDragable.dragable)
            {
                // 确认拖放对象放到了一个格子
                dropDragable.draggedToSlot = true;

                if (dropDragable != this)
                {
                    int from = dropDragable.name.ToInt();
                    int to = name.ToInt();
                    SendMessage("OnDragAndDrop_" + dropDragable.tag + "_" + tag,
                        new int[]{from, to,dropDragable.itemCat},
                        SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    void OnDisable()
    {
        Destroy(currentlyDragged);
    }

    void OnDestroy()
    {
        Destroy(currentlyDragged);
    }
}
