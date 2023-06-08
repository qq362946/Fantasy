using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Handler;

public sealed class H_C2Chat_GetTest_ReqHandler : RouteRPC<Unit, H_C2Chat_GetTest_Req, H_Chat2C_GetTest_Res>
{
    protected override async FTask Run(Unit unit, H_C2Chat_GetTest_Req request, H_Chat2C_GetTest_Res response, Action reply)
    {
        Log.Debug($"H_C2Chat_GetTest_Req unitId:{unit.Id} name:{unit.Name} messageName:{request.Name}");
        response.ResultName = "Fantasy 123123123123 C2Chat";
        await FTask.CompletedTask;
    }
}