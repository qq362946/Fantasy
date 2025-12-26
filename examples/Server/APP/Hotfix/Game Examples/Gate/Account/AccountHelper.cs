using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;

namespace Fantasy;

public static class AccountHelper
{
    public static async FTask<uint> Online(Session session, Account account)
    {
        // 1.创建Roaming协议,用来方便的把消息通过Gate中转到Map
        
        var createRoamingResult = await session.TryCreateRoaming(account.Id, true, 6000);

        switch (createRoamingResult.Status)
        {
            case CreateRoamingStatus.NewCreated:
            {
                // 2.首先登陆到Map
                // 正常情况下这个Map需要根据玩家所在的地图来选择的。
                // 因为这个例子没有划分地图，所以就默认拿第一来当做地图。
                var mapSceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                // 创建一个Gate中间Map消息的连接，创建成功后后面的所有消息都会自动中转无需手动转发，包括MAP发送给Client
                var linkResuilt = await createRoamingResult.Roaming.Link(session, mapSceneConfig, RoamingType.MapRoamingType);
                if (linkResuilt != 0)
                {
                    Log.Error($"创建Map的漫游消息发生了错误 ErrorCode:{linkResuilt}");
                    return linkResuilt;
                }
                break;
            }
            case CreateRoamingStatus.AlreadyExists:
            {
                // 如果已经存在代表当前是断线重连，可以不需要做任何处理就可以了。因为漫游系统还存在。
                break;
            }
            case CreateRoamingStatus.SessionAlreadyHasRoaming:
            {
                // 表示当前Session已经建立了漫游，并且漫游的ID是另外一个ID，这个情况是不允许的。
                // 如果出现这个情况肯定是你设计的问题了，请仔细检查一下。
                break;
            }
        }
        
        return 0;
    }

    public static async FTask Offline(Scene scene, Account account)
    {
        await FTask.CompletedTask;
    }
}