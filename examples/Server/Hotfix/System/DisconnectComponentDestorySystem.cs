using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Hotfix.System
{
    internal class DisconnectComponentSystem : DestroySystem<DisconnectComponent>
    {
        protected override void Destroy(DisconnectComponent self)
        {
            var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
            self.Scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new G2M_RemoveClient() { ClientID = self.ClientId });
            Log.Debug("客户端销毁");
        }
    }
}
