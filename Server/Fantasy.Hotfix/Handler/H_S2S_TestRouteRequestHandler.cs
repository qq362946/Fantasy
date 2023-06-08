// using Fantasy.Core.Network;
//
// namespace Fantasy.Hotfix.Handler;
//
// public class H_S2S_TestRouteRequestHandler : RouteRPCMessageHandler<Scene, H_S2S_TestRouteRequest, H_S2S_TestRouteResponse>
// {
//     protected override async FTask Run(Scene scene, H_S2S_TestRouteRequest request, H_S2S_TestRouteResponse response, Action reply)
//     {
//         Log.Debug($"Request Server:{scene.ServerId} Account:{request.Account}");
//         response.Account = "9991";
//         await FTask.CompletedTask;
//     }
// }