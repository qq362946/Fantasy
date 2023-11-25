using UnityEngine;
using System.Linq;
using PlatformCharacterController;
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
        Player prefab = classes.ToList().Find(p => p.ClassName == className);

        if (prefab != null)
        {
            GameObject go =  base.TryPool(className,"CV");
            go.transform.position = point.position;
            go.transform.rotation = point.rotation;
        }else Debug.LogError("no prefab found for class: " + className);

        return null;
    }
}