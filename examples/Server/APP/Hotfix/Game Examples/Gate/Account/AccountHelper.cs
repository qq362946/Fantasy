using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy;

public static class AccountHelper
{
    /// <summary>
    /// 上线操作
    /// </summary>
    /// <param name="session"></param>
    /// <param name="account"></param>
    /// <returns></returns>
    public static async FTask<uint> Online(Session session, Account account)
    {
        // 1.创建Roaming协议,用来方便的把消息通过Gate中转到Map
        var roamingComponent = await session.GetOrCreateRoaming(account.Id, 6000);
        // 首次连接选择Map，重连使用漫游连接中保存的目标Scene地址。
        var linkResuilt = roamingComponent.IsLinked(RoamingType.MapRoamingType)
            ? await roamingComponent.Link(RoamingType.MapRoamingType)
            : await roamingComponent.Link(
                SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0].Address,
                RoamingType.MapRoamingType);
        if (linkResuilt != 0)
        {
            Log.Error($"创建Map的漫游消息发生了错误 ErrorCode:{linkResuilt}");
            return linkResuilt;
        }
        account.Session = session;
        return 0;
    }

    /// <summary>
    /// 下线操作
    /// </summary>
    /// <param name="account"></param>
    public static async FTask Offline(Account account)
    {
        if (!RoamingHelper.TryGetRoaming(account.Scene, account.Id, out var roaming))
        {
            // 如果没有查询到Roaming就不需要继续进行下面的逻辑了。
            return;
        }

        // 发送给Map进行下线操作
        var g2MOfflineRequest = G2M_OfflineRequest.Create();
        // 这里可以增加一下延迟下线的时间，但本例子没有实现，需要的话可以自己实现下
        g2MOfflineRequest.OfflineTime = 0;
        // 发送给Map的漫游消息
        var response = await roaming.Call(g2MOfflineRequest);
        if (response.ErrorCode != 0)
        {
            Log.Error($"下线失败 看到这个日志一定要解决这个问题 ErrorCode:{response.ErrorCode}");
        }
        // 移除Account
        AccountManageHelper.Remove(account.Scene, account.Name);
    }
}
