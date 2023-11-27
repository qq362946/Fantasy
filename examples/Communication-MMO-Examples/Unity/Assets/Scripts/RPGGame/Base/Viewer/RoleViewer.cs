using UnityEngine;
using System.Linq;
using PlatformCharacterController;
/// <summary>
/// 玩家角色预览
/// </summary>
public class RoleViewer : BaseViewer
{
    private Player[] classes;

    public void Awake()
    {
        rootName = "RoleViewRoot";
        classes = GameManager.Ins.playerClasses.ToArray();
    }

    // 预览角色要用roleId缓存
    public GameObject ViewRole(string className,string roleId,Transform point)
    {
        Player prefab = classes.ToList().Find(p => p.ClassName == className);

        if (prefab != null)
        {
            GameObject go =  base.TryPool(className,"RV"+roleId);
            go.transform.position = point.position;
            go.transform.rotation = point.rotation;
        }else Debug.LogError("no prefab found for class: " + className);
        
        return null;
    }
}