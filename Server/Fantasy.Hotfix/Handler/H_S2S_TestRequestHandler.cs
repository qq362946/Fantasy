// using Fantasy.Core.Network;
//
// namespace Fantasy.Hotfix.Handler;
//
// public sealed class H_S2S_TestRequestHandler : RPCMessageHandler<H_S2S_TestRequest,H_S2S_TestResponse>
// {
//     protected override async FTask Run(Session session, H_S2S_TestRequest request, H_S2S_TestResponse response, Action reply)
//     {
//         Log.Debug($"Request Server:{session.Scene.ServerId} Account:{request.Account}");
//         response.Account = "999";
//         await FTask.CompletedTask;
//     }
// }