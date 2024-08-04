#if FANTASY_NET
namespace Fantasy;

public struct OnCreateScene
{
    /// <summary>
    /// 获取与事件关联的场景实体。
    /// </summary>
    public readonly Scene Scene;
    /// <summary>
    /// 初始化一个新的 OnCreateScene 实例。
    /// </summary>
    /// <param name="scene"></param>
    public OnCreateScene(Scene scene)
    {
        Scene = scene;
    }
}
#endif