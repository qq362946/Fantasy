// using Fantasy.Core.Network;
//
// namespace Fantasy.Hotfix.Handler;
//
// public sealed class H_S2S_TestRouteMessageHandler : RouteMessageHandler<Scene, H_S2S_TestRouteMessage>
// {
//     protected override async FTask Run(Scene scene, H_S2S_TestRouteMessage message)
//     {
//         Log.Debug($"RouteMessage scene:{scene.Name} Server:{scene.Server.Id} message:{message.ToJson()}");
//         await FTask.CompletedTask;
//     }
// }