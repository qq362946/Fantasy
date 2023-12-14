using UnityEngine;
using System.Linq;
using MicroCharacterController;
/// <summary>
/// 职业预览
/// </summary>
public class ClassViewer : BaseViewer
{
    private Player[] classes;

    public void Awake()
    {
        rootName = "ClassViewRoot";
        classes = GameManager.Ins.playerClasses.ToArray();
    }

    public GameObject ViewClass(string className,Transform point)
    {
        GameObject go =  base.TryPool(PoolType.Unit,className,"CV"+className);
        go.transform.position = point.position;
        go.transform.rotation = point.rotation;
        return go;
    }
}