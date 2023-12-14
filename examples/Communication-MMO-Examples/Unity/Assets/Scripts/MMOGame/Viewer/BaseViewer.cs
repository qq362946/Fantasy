using UnityEngine;
using System.Linq;
using MicroCharacterController;
/// <summary>
/// 角色职业预览
/// </summary>
public class BaseViewer :MonoBehaviour
{
    protected GameObject viewRoot;
    [HideInInspector]public GameObject current;
    public string rootName = "ViewRoot";
    public bool isMulti = false;

    public GameObject TryPool(PoolType type,string className,string ex = "")
    {
        if(viewRoot == null) viewRoot = new GameObject(rootName);
        if(current != null && !isMulti) GameFacade.Ins.Push(type,current.name,current);

        var name = string.IsNullOrWhiteSpace(ex)?className:ex;
        GameObject go = GameFacade.Ins.TryGet(type,className,name);
        go.transform.parent = viewRoot.transform;
        current = go;

        return go;
    }
    
    // 通过缓存清除预览
    public virtual void ClearPreview(PoolType type)
    {
        if(viewRoot == null) return;
        foreach (Transform child in viewRoot.transform)
        {
            GameFacade.Ins.Push(type,child.name,child.gameObject);
        }
        Destroy(viewRoot);
    }

    // 不缓存直接销毁
    public void ClearPreview()
    {
        if(viewRoot == null) return;
        // 遍历所有子物体
        for (int i = viewRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = viewRoot.transform.GetChild(i);
            // 销毁当前子物体
            Destroy(child.gameObject);
        }
        Destroy(viewRoot);
    }
}