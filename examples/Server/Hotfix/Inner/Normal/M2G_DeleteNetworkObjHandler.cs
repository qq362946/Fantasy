using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_DeleteNetworkObjHandler : Route<Scene, M2G_DeleteNetworkObj>
    {
        protected override async FTask Run(Scene scene, M2G_DeleteNetworkObj message)
        {
            var session = scene.GetEntity<Session>(message.ClientID);
            if (session != null)
            {
                Log.Debug($"通知{message.ClientID}移除单位:{message.NetworkObjectID}");
                session.Send(new G2C_DeleteNetworkObj()
                {
                    NetworkObjectID = message.NetworkObjectID,
                });
            }
            else
            {
                var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new G2M_RemoveClient() { ClientID = message.ClientID });
            }

            await FTask.CompletedTask;
        }
    }
}