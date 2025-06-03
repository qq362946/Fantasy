using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;
using Fantasy.Roaming;

namespace Fantasy;

public class C2Map_TestTransferRequestHandler : RoamingRPC<Terminus, C2Map_TestTransferRequest, Map2C_TestTransferResponse>
{
    protected override async FTask Run(Terminus terminus, C2Map_TestTransferRequest request, Map2C_TestTransferResponse response, Action reply)
    {
        Log.Debug($"C2Map_TestTransferRequestHandler1 terminus:{terminus.RuntimeId}");
        var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[1];
        response.ErrorCode = await terminus.StartTransfer(mapConfig.RouteId);
    }
}