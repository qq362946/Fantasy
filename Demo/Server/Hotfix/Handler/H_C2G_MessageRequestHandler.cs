using Fantasy.Core.Network;
using Fantasy.Helper;

namespace Fantasy.Hotfix;

public class H_C2G_MessageRequestHandler : MessageRPC<H_C2G_MessageRequest,H_G2C_MessageResponse>
{
    protected override async FTask Run(Session session, H_C2G_MessageRequest request, H_G2C_MessageResponse response, Action reply)
    {
        // 这里是接收到客户端发送的消息
        Log.Debug($"接收到RPC消息 H_C2G_MessageRequest {TimeHelper.Now}");
        Log.Debug($"接收到RPC消息 H_C2G_MessageRequest:{request.ToJson()}");
        // response是要给客户端返回的消息、数据结构是在proto文件里定义的
        response.Message = "欢迎使用Fantasy，您现在收到的消息是一个RPC消息";
        await FTask.CompletedTask;
    }
}