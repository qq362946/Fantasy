using UnityEngine;

/// <summary>
/// UIPanel资源工厂
/// </summary>
public class UIPanelPool : BasePool
{
    private static UIPanelPool _instance = null;
    public static UIPanelPool Instance
    {
        get{return _instance;}
    }

    static UIPanelPool()
    {
        _instance = new UIPanelPool();
        _instance.loadPath += "UIPanel/";
        _instance.itemPool = new GameObject("UIPanel");
    }
}
