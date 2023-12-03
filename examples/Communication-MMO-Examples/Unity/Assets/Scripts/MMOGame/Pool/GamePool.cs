using UnityEngine;

/// <summary>
/// 游戏物体(怪物,塔,子弹,道具等)工厂
/// </summary>
public class GamePool : BasePool
{
    private static GamePool _instance = null;
    public static GamePool Instance
    {
        get{return _instance;}
    }

    static GamePool()
    {
        _instance = new GamePool();
        _instance.loadPath += "Game/";
        _instance.itemPool = new GameObject("GamePool");
    }
}
