#if FANTASY_NET
namespace Fantasy.Core.Network;

/// <summary>
/// 声明一个 sealed 类 I_AddressableGetHandler，继承自 RouteRPC 类，并指定泛型参数
/// </summary>
public sealed class I_AddressableGetHandler : RouteRPC<Scene, I_AddressableGet_Request, I_AddressableGet_Response>
{
    /// <summary>
    /// 在收到地址映射获取请求时执行的逻辑。
    /// </summary>
    /// <param name="scene">当前场景实例。</param>
    /// <param name="request">包含请求信息的 I_AddressableGet_Request 实例。</param>
    /// <param name="response">用于构建响应的 I_AddressableGet_Response 实例。</param>
    /// <param name="reply">执行响应的回调操作。</param>
    protected override async FTask Run(Scene scene, I_AddressableGet_Request request, I_AddressableGet_Response response, Action reply)
    {
        response.RouteId =  await scene.GetComponent<AddressableManageComponent>().Get(request.AddressableId);
    }
}
#endif
