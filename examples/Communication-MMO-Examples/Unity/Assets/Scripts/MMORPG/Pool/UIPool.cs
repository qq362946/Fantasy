using UnityEngine;

/// <summary>
/// UI资源工厂池
/// </summary>
public class UIPool : BasePool
{
    private static UIPool _instance = null;
    public static UIPool Instance
    {
        get{return _instance;}
    }

    static UIPool()
    {
        _instance = new UIPool();
        _instance.loadPath += "UI/";
        _instance.itemPool = new GameObject("UIPool");
    }
}