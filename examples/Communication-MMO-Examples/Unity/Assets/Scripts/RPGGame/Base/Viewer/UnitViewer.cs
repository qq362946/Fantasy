using UnityEngine;
using System.Linq;
using PlatformCharacterController;
/// <summary>
/// 角色查看器
/// </summary>
public class UnitViewer : BaseViewer
{
    private UnitBase[] classes;

    public void Awake()
    {
        rootName = "UnitViewRoot";
        isMulti = true;
        classes = GameManager.Ins.unitClasses.ToArray();
    }

    // 角色要用roleId缓存
    public GameObject ViewUnit(string className,string roleId,Transform point)
    {
        UnitBase prefab = classes.ToList().Find(p => p.ClassName == className);

        if (prefab != null)
        {
            GameObject go =  base.TryPool(className,"UV"+roleId);
            go.transform.position = point.position;
            go.transform.rotation = point.rotation;
            return go;
        }else Debug.LogError("no prefab found for class: " + className);
        
        return null;
    }
}