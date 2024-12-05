using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network.Interface;

namespace Fantasy;

public class G2M_CreateSubSceneRequestHandler : RouteRPC<Scene, G2M_CreateSubSceneRequest, M2G_CreateSubSceneResponse>
{
    protected override async FTask Run(Scene scene, G2M_CreateSubSceneRequest request, M2G_CreateSubSceneResponse response, Action reply)
    {
        // 下面的SceneType传的是666，其实并没有这个类型，这个是我随便写的。
        var subScene = Scene.CreateSubScene(scene, 6666);
        // 返回subScene的运行时id
        response.SubSceneRouteId = subScene.RuntimeId;
        await FTask.CompletedTask;
    }
}