using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
public class UIUtils
{
    // 删除道具时,即使删除Cotent下的格子物体,还是会再创建新的格子实例
    public static void BalancePrefabs(GameObject prefab, int amount, Transform parent)
    {
        
        //在父项下实例化直到达到amount数量
        //体会为什么在这里必须int i = parent.childCount
        for (int i = parent.childCount; i < amount; ++i)
        {
            GameObject.Instantiate(prefab, parent, false);
        }

        //删除过多的内容
        //（向后循环，因为Destroy更改了childCount）
        for (int i = parent.childCount-1; i >= amount; --i)
            GameObject.Destroy(parent.GetChild(i).gameObject);
    }
    public static void BalancePrefabs(GameObject prefab, Transform location,Transform hideItems)
    {
        for (int i = hideItems.childCount; i < location.childCount; ++i)
        {
            //Debug.Log(location.childCount);
            GameObject locGo = location.GetChild(i).gameObject;
            GameObject go =  GameObject.Instantiate(prefab, locGo.transform, false);
            GameObject goHide = new GameObject();
            goHide.transform.SetParent(hideItems);
        }

    }
    

    // 使用Selectable.all查找当前是否有任何输入处于活动状态
    public static bool AnyInputActive()
    {
        // 避免Linq.Any 不利于GC与性能!
        foreach (Selectable sel in Selectable.allSelectablesArray)
            if (sel is InputField inputField && inputField.isFocused)
                return true;
        return false;
    }

    // 严谨的取消选择UI元素
    //（点击有些地方时，会抛出错误，所以我们必须双重检查）
    public static void DeselectCarefully()
    {
        if (!Input.GetMouseButton(0) &&
            !Input.GetMouseButton(1) &&
            !Input.GetMouseButton(2))
            EventSystem.current.SetSelectedGameObject(null);
    }

    public bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
