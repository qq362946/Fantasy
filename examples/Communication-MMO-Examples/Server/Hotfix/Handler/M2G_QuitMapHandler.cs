using Fantasy;

namespace BestGame;

public class M2G_QuitMapHandler : Route<Scene,M2G_QuitMapMsg>
{
    protected override async FTask Run(Scene scene,M2G_QuitMapMsg message)
    {
        var gateAccountManager = scene.GetComponent<GateAccountManager>();

        if (gateAccountManager.TryGetValue(message.AccountId, out GateAccount gateAccount))
        {
            var gateRole = gateAccount.GetCurRole();

            // 记录最后进入的地图
            gateRole.LastMap = message.MapNum;
            
            gateAccountManager.QuitGame(gateAccount); 

            await AddressableHelper.RemoveAddressable(scene,gateAccount.AddressableId);
        }
    }
}