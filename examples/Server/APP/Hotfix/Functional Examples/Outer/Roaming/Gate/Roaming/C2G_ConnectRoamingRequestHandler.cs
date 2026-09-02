using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Model.Roaming;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;

namespace Fantasy;

/// <summary>
/// 演示客户端连接 Gate 后，如何为同一个 Session 建立或恢复多条漫游连接。
/// </summary>
/// <remarks>
/// 本示例会分别连接 Map 和 Chat。连接成功后，客户端发给 Gate 的漫游消息会按照协议配置的
/// RoamingType 自动转发到对应 Scene；目标 Scene 发给客户端的消息也会通过 Gate 转发，
/// 业务代码不需要再手动编写中转消息。
/// </remarks>
public sealed class C2G_ConnectRoamingRequestHandler : MessageRPC<C2G_ConnectRoamingRequest, G2C_ConnectRoamingResponse>
{
    protected override async FTask Run(
        Session session,
        C2G_ConnectRoamingRequest request,
        G2C_ConnectRoamingResponse response,
        Action reply)
    {
        // 第一步：为当前 Session 获取漫游上下文。
        //
        // roamingId 是断线重连前后保持不变的业务身份。这里为了演示固定写成 1；
        // 正式项目通常应使用玩家 ID、角色 ID 等能够唯一标识登录对象的稳定 ID。
        //
        // delayRemove 设置为 10_000 毫秒，表示 Session 断开后，框架会继续保留这个漫游上下文 10 秒：
        // 1. 客户端在 10 秒内使用相同 roamingId 重连时，会复用原来的 Map、Chat 等漫游关系；
        // 2. 框架会把漫游上下文重新绑定到新 Session，后续 Link 会使用新 Session 的 Address；
        // 3. 超过保留时间仍未重连，框架才会断开目标 Terminus 并销毁漫游上下文。
        var roamingComponent = await session.GetOrCreateRoaming(
            roamingId: 1,
            delayRemove: 10_000);

        // Link 返回 0（InnerErrorCode.Success）表示成功，非 0 表示框架内部错误码。
        // 这个变量会依次保存 Map 和 Chat 的连接结果；每一步都会立即检查，失败后不再继续。
        var linkResponse = InnerErrorCode.Success;

        // 第二步：建立或恢复 Map 漫游连接。
        //
        // 一个 SessionRoamingComponent 可以同时保存多种 roamingType 的连接。
        // IsLinked(MapRoamingType) 只判断 Map 连接是否存在，不代表 Chat 等其他连接也存在。
        // RoamingType 来自网络协议目录中的 RoamingType.Config；消息协议配置相同类型后，
        // Gate 才知道该消息应该通过这条 Map 漫游连接转发。
        if (!roamingComponent.IsLinked(RoamingType.MapRoamingType))
        {
            // Map 连接不存在，说明这是该 roamingType 的首次连接。
            // 只有首次连接才需要选择目标服务器；重连时必须继续使用原来保存的目标 Scene，
            // 否则重新取配置可能把同一个玩家错误地连接到另一台 Map。
            //
            // 本示例没有地图分片和玩家归属逻辑，所以直接选择配置中的第一个 Map。
            // 正式项目应根据玩家所在地图、分区或业务绑定关系选择正确的 Map Scene。
            var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
            var mapSceneAddress = mapConfig.Address;

            // args 是随 Link 请求发送给目标 Scene 的可选业务参数。
            // 参数类型必须继承 Entity，同时使用 [MemoryPackable] 并声明为 partial，才能被内部协议序列化。
            // using var 负责释放 Gate 侧创建的原始对象；目标 Scene 反序列化得到的是另一份对象，
            // 必须在 OnCreateTerminus 事件中使用完后单独 Dispose，不能依赖这里的 using 释放。
            using var args = Entity.Create<MaoRoamingArgs>(session.Scene);
            args.Tag = "HI";

            // 带 targetSceneAddress 的 Link 用于首次连接。
            // SessionRoamingComponent 会自动使用当前绑定 Session 的 Address 作为回传地址，
            // 因此调用方不需要也不应该手动传入 session.Address。
            // 目标 Map 会创建 Terminus，并触发 CreateTerminusType.Link 的 OnCreateTerminus 事件。
            linkResponse = await roamingComponent.Link(mapSceneAddress, RoamingType.MapRoamingType, args);
        }
        else
        {
            // Map 连接仍然存在，说明客户端是在延迟销毁期限内重连。
            // 此时不再查询或选择 Map；Link(roamingType, args) 会直接使用组件保存的目标 Scene 地址，
            // 同时使用重连后的新 Session 地址恢复转发，并在目标端触发 CreateTerminusType.ReLink 事件。
            // 业务层可以在 ReLink 事件里恢复玩家在线状态，而不必重新创建整条漫游关系。
            using var args = Entity.Create<MaoRoamingArgs>(session.Scene);
            args.Tag = "HI";
            linkResponse = await roamingComponent.Link(RoamingType.MapRoamingType, args);
        }

        if (linkResponse != 0)
        {
            response.ErrorCode = linkResponse;
            Log.Debug($"Map 漫游连接或恢复失败 ErrorCode:{linkResponse}");
            return;
        }

        // 第三步：建立或恢复 Chat 漫游连接。
        //
        // Map 和 Chat 共用同一个 SessionRoamingComponent，但通过不同的 roamingType 分别保存连接。
        // 同一个 roamingType 在一个漫游上下文中只能对应一条连接；不同 roamingType 则可以同时存在。
        // Chat 本例不需要额外业务参数，所以省略 args。
        if (!roamingComponent.IsLinked(RoamingType.ChatRoamingType))
        {
            // Chat 首次连接同样需要先选择目标 Scene。
            // 本示例直接选择配置中的第一个 Chat；生产环境可以使用服务发现或一致性哈希选择实例。
            var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
            var chatConfigAddress = chatConfig.Address;

            linkResponse = await roamingComponent.Link(chatConfigAddress, RoamingType.ChatRoamingType);
        }
        else
        {
            // Chat 连接仍然存在时直接重连，不重新选择服务器。
            // 框架会复用已保存的 Chat Scene 地址，并让目标端收到 ReLink 事件。
            linkResponse = await roamingComponent.Link(RoamingType.ChatRoamingType);
        }

        if (linkResponse != 0)
        {
            response.ErrorCode = linkResponse;
            Log.Debug($"Chat 漫游连接或恢复失败 ErrorCode:{linkResponse}");
            return;
        }

        // 两条连接都完成后，MapRoamingType 和 ChatRoamingType 对应的消息即可自动转发。
        Log.Debug("Map 和 Chat 漫游连接成功");
    }
}
