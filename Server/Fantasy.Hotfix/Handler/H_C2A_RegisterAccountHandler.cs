using System.Diagnostics;
using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Handler;

public sealed class H_C2A_RegisterAccountHandler : MessageRPC<H_C2A_RegisterAccount, H_A2C_RegisterAccount>
{
    // private static int Index;
    protected override async FTask Run(Session session, H_C2A_RegisterAccount request, H_A2C_RegisterAccount response, Action reply)
    {
        Log.Debug($"Server:{session.Scene.RouteId} {request.ToJson()}");

        response.Account = "Fantasy";
        // var sceneId = SceneConfigData.Instance.MapScenes[0].EntityId;
        // var loginResponse = (I_M2GLoginResponse)await MessageHelper.CallInnerRoute(session.Scene, sceneId,
        //     new I_G2MLoginRequest()
        //     {
        //         AccountId = ++Index
        //     });
        //
        // if (loginResponse.ErrorCode != 0)
        // {
        //     Log.Error($"I_G2MLoginRequest error ErrorCode:{loginResponse.ErrorCode}");
        // }
        //
        // var addressableRouteComponent = session.AddComponent<AddressableRouteComponent>(123);
        // addressableRouteComponent.SetAddressableRouteId(loginResponse.AddressRouteId);
        //
        // var routeComponent = session.AddComponent<RouteComponent>();
        //
        // var chatSceneId = SceneConfigData.Instance.ChatScene.EntityId;
        //
        // var chatLoginResponse = (I_M2ChatLoginResponse)await MessageHelper.CallInnerRoute(session.Scene, chatSceneId,
        //     new I_G2ChatLoginRequest()
        //     {
        //         AccountId = ++Index
        //     });
        //
        // if (chatLoginResponse.ErrorCode != 0)
        // {
        //     Log.Error($"I_G2ChatLoginRequest error ErrorCode:{chatLoginResponse.ErrorCode}");
        // }
        // else
        // {
        //     routeComponent.AddAddress((int)RouteType.ChatRoute, chatLoginResponse.AddressRouteId);
        // }
        //
        // response.Account = "Fantasy";
        await FTask.CompletedTask;
    }
}