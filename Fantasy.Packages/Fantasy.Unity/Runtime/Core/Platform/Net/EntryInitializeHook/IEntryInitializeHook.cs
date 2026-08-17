#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// Entry 初始化钩子接口
/// 在 Entry.Initialize() 期间调用，用于自定义启动前逻辑
/// </summary>
public interface IEntryInitializeHook : Fantasy.Assembly.ICustomInterface
{
    /// <summary>
    /// 在配置加载后、序列化器初始化前调用
    /// </summary>
    Task OnInitialize();
}
#endif