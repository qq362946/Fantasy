using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class C2G_LoginGameRequestHandler : MessageRPC<C2G_LoginGameRequest,G2C_LoginGameResponse>
{
    protected override async FTask Run(Session session, C2G_LoginGameRequest request, G2C_LoginGameResponse response, Action reply)
    {
        var accountName = request.AccountName;

        if (string.IsNullOrEmpty(accountName))
        {
            // 懒的写错误码，所以只要错误码不是0就是出错了。
            // 想细化各种情况的可以自己加错误码。
            // 其实应该是要用配置表来做一个错误码列表用于前后端查询错误码使用的。
            // 这里只解释一次，后面有错误码的直接使用不会再加注释了。
            response.ErrorCode = 1;
            return;
        }

        if (!AccountManageHelper.Add(session.Scene, accountName, out var account))
        {
            response.ErrorCode = 1;
            return;
        }
session.RestartIdleChecker();
        account.Session = session;

        // 执行上线流程
        await AccountHelper.Online(session, account);
        
        if (!session.TryGetRoaming(out var roamingComponent))
        {
            return;
        }
        
        roamingComponent.Send(RoamingType.MapRoamingType,new G2M_PalyerJoin()
        {
            Sex = "6666"
        });
    }
}