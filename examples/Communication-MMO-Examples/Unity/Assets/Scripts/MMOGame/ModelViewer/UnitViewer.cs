using UnityEngine;
using System.Linq;
using MicroCharacterController;
/// <summary>
/// 角色查看器
/// </summary>
public class UnitViewer : BaseViewer
{
    private GameEntity[] classes;

    public void Awake()
    {
        rootName = "UnitViewRoot";
        classes = GameManager.Ins.unitClasses.ToArray();
    }

    // 角色要用roleId缓存
    public GameObject ViewUnit(string className,string roleId,Transform point)
    {
        GameObject go =  base.TryPool(PoolType.Unit,className,roleId);
        go.transform.position = point.position;
        go.transform.rotation = point.rotation;
        return go;
    }

}