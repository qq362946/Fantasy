using UnityEngine;
using System.Linq;
using PlatformCharacterController;
/// <summary>
/// 角色职业预览
/// </summary>
public class BaseViewer :MonoBehaviour
{
    protected GameObject viewRoot;
    [HideInInspector]public GameObject current;
    public string rootName = "ViewRoot";

    public GameObject TryPool(string className,string ex = "")
    {
        if(viewRoot == null) viewRoot = new GameObject(rootName);
        if(current != null) GameManager.Ins.PushUnit(current.name,current);

        GameObject go = GameManager.Ins.GetUnit(className,ex + className);
        go.transform.parent = viewRoot.transform;
        go.name = ex + className;
        current = go;

        return go;
    }
    
    public virtual void ClearPreview(){
        GameManager.Ins.PushUnit(current.name,current);
        Destroy(viewRoot);
    }
}