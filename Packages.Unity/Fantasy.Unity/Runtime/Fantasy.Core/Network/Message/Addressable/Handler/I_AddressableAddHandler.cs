#if FANTASY_NET
namespace Fantasy;

/// <summary>
/// 声明一个 sealed 类 I_AddressableAddHandler，继承自 RouteRPC 类，并指定泛型参数
/// </summary>
public sealed class I_AddressableAddHandler : RouteRPC<Scene, I_AddressableAdd_Request, I_AddressableAdd_Response>
{
    /// <summary>
    /// 在收到地址映射添加请求时执行的逻辑。
    /// </summary>
    /// <param name="scene">当前场景实例。</param>
    /// <param name="request">包含请求信息的 I_AddressableAdd_Request 实例。</param>
    /// <param name="response">用于构建响应的 I_AddressableAdd_Response 实例。</param>
    /// <param name="reply">执行响应的回调操作。</param>
    protected override async FTask Run(Scene scene, I_AddressableAdd_Request request, I_AddressableAdd_Response response, Action reply)
    {
        await scene.GetComponent<AddressableManageComponent>().Add(request.AddressableId, request.RouteId, request.IsLock);
    }
}
#endif