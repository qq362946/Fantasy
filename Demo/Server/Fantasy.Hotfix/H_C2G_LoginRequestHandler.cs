using Fantasy.Core.Network;

namespace Fantasy.Hotfix;

public class H_C2G_LoginRequestHandler : MessageRPC<H_C2G_LoginRequest,H_G2C_LoginResponse>
{
    protected override async FTask Run(Session session, H_C2G_LoginRequest request, H_G2C_LoginResponse response, Action reply)
    {
        Log.Debug($"接收到消息 UserName:{request.UserName} Password:{request.Password}");
        response.Text = "接收到消息";
        await FTask.CompletedTask;
    }
}