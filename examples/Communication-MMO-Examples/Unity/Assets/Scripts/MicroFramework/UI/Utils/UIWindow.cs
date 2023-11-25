using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//向UI面板添加类似窗口的行为，以便可以移动和关闭它们

public enum CloseOption
{
    DoNothing,
    DeactivateWindow,
    DestroyWindow
}

public class UIWindow : BehaviourNonAlloc, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CloseOption onClose = CloseOption.DeactivateWindow;
    public static UIWindow currentlyDragged;

    // cache
    Transform window;

    void Awake()
    {
        // 缓存父窗口
        window = transform.parent;
    }

    public void HandleDrag(PointerEventData d)
    {
        // 发送OnWindowDrag消息,可能其它组件需要
        window.SendMessage("OnWindowDrag", d, SendMessageOptions.DontRequireReceiver);

        // 移动父物体
        window.Translate(d.delta);
    }

    public void OnBeginDrag(PointerEventData d)
    {
        currentlyDragged = this;
        HandleDrag(d);
    }

    public void OnDrag(PointerEventData d)
    {
        HandleDrag(d);
    }

    public void OnEndDrag(PointerEventData d)
    {
        HandleDrag(d);
        currentlyDragged = null;
    }

    public void OnClose()
    {
        // 发送OnWindowClose消息,可能其它组件需要
        window.SendMessage("OnWindowClose", SendMessageOptions.DontRequireReceiver);

        // 隐藏窗口
        if (onClose == CloseOption.DeactivateWindow)
            window.gameObject.SetActive(false);

        // destroy
        if (onClose == CloseOption.DestroyWindow)
            Destroy(window.gameObject);
    }
}
