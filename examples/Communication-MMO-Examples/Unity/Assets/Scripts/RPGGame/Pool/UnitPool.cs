using UnityEngine;

/// <summary>
/// 游戏物体(怪物,塔,子弹,道具等)工厂
/// </summary>
public class UnitPool : BasePool
{
    private static UnitPool _instance = null;
    public static UnitPool Instance
    {
        get{return _instance;}
    }

    static UnitPool()
    {
        _instance = new UnitPool();
        _instance.loadPath += "Unit/";
        _instance.itemPool = GameObject.Find("UnitPool");
    }
}
