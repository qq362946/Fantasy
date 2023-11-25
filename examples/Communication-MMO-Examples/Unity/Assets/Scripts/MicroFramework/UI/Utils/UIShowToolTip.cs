using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 当光标位于此UI元素上时实例化工具提示。
public class UIShowToolTip : BehaviourNonAlloc, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 绑定tootip预制体
    /// </summary>
    public GameObject tooltipPrefab;
    [TextArea(1, 30)] public string text = "";

    /// <summary>
    /// 获取tooltip 实例
    /// </summary>
    GameObject current;

    void Update()
    {
        // 实时将文本复制到工具提示。
        if (current) current.GetComponentInChildren<Text>().text = text;
    }


    public void OnPointerEnter(PointerEventData d)
    {
        // 鼠标经过按钮显示提示
        ShowToolTip(0.5f);
    }

    public void OnPointerExit(PointerEventData d)
    {
        // 鼠标离开按钮消除提示
        DestroyToolTip();
    }

    void OnDisable()
    {
        DestroyToolTip();
    }

    void OnDestroy()
    {
        DestroyToolTip();
    }

    void CreateToolTip()
    {
        current = Instantiate(tooltipPrefab, transform.position, Quaternion.identity);

        // UI物体的root就是Canvas
        current.transform.SetParent(transform.root, true); 
        current.transform.localScale = new Vector3(1f,1f,1f);

        // 将tooltip移动到层级的最末尾，这样就会显示在最前面
        current.transform.SetAsLastSibling(); 

        current.GetComponentInChildren<Text>().text = text;
    }

    void ShowToolTip(float delay)
    {
        // 使用Invoke执行方法，和进行延时显示
        Invoke(nameof(CreateToolTip), delay);
    }

    public bool IsVisible() => current != null;

    void DestroyToolTip()
    {
        // 有可能还在延时还没显示，鼠标就离开按钮了，所以需要先取消Invoke再Destroy
        CancelInvoke(nameof(CreateToolTip));

        Destroy(current);
    } 
}