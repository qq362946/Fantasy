#if FANTASY_NET

// ReSharper disable CollectionNeverQueried.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy;

/// <summary>
/// 连接其他进程实体地址管理组件
/// </summary>
public sealed class LinkEntityComponent : Entity
{
    /// <summary>
    /// 连接的Gate的Session的RuntimeId、通过这个可以直接发送消息给客户端
    /// </summary>
    public long LinkGateSessionRuntimeId;
    /// <summary>
    /// 其他已经连接其他进程的Entity地址、key为EntityType类型、value为Entity所在进程的RouteId
    /// </summary>
    public readonly Dictionary<int, long> LinkEntity = new Dictionary<int, long>();

    /// <summary>
    /// 销毁
    /// </summary>
    public override void Dispose()
    {
        LinkGateSessionRuntimeId = 0;
        LinkEntity.Clear();
        base.Dispose();
    }
}

public sealed class ConnectEntityHandler : RouteRPC<Entity, LinkEntity_Request, LinkEntity_Response>
{
    protected override async FTask Run(Entity entity, LinkEntity_Request request, LinkEntity_Response response, Action reply)
    {
        var connectEntityComponent = entity.GetComponent<LinkEntityComponent>() ?? entity.AddComponent<LinkEntityComponent>();

        if (request.LinkGateSessionRuntimeId == 0)
        {
            connectEntityComponent.LinkEntity[request.EntityType] = request.RuntimeId;
        }
        else
        {
            connectEntityComponent.LinkGateSessionRuntimeId = request.LinkGateSessionRuntimeId;
        }

        await FTask.CompletedTask;
    }
}
#endif