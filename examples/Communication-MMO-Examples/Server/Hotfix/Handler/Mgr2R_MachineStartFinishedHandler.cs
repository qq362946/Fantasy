using Fantasy;

namespace BestGame;

/// <summary>
/// 调用AccountManager组件，初始化网关列表与更新网关访问状态
/// </summary>
public class Mgr2R_MachineStartFinishedHandler : Route<Scene,Mgr2R_MachineStartFinished>
{
    protected override async FTask Run(Scene scene,Mgr2R_MachineStartFinished message)
    {
        var accountManager = scene.GetComponent<AccountManager>();
        await accountManager.MachineStartFinished();
    }
}

