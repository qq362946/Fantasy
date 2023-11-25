/// <summary>
/// 不同种类的游戏物体工厂的枚举
/// </summary>
public enum PoolType
{
    UIPanel,     // 加载UI面板的工厂类型 
    UI,         //  加载游戏中动态UI的工厂类型 
    Unit,       //  加载游戏中角色的工厂类型
    Game //   游戏进行时产生的游戏物体(怪物,塔,子弹,道具等)工厂类型
}