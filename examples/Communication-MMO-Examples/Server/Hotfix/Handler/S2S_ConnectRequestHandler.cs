using Fantasy;

namespace BestGame;
public class S2S_ConnectRequestHandler : MessageRPC<S2S_ConnectRequest, S2S_ConnectResponse>
{
    // 内网连接Handler
    protected override async FTask Run(Session session, S2S_ConnectRequest request, S2S_ConnectResponse response, Action reply)
    {
        Log.Info($"{request.Key} 连接 {AppDefine.Options.AppId}");
        response.Key = (int)RandomHelper.RandUInt32();

        await FTask.CompletedTask;
    }
}


