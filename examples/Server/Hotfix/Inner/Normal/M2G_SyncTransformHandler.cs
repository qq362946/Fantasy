using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_SyncTransformHandler : Route<Scene, M2G_SyncTransform>
    {
        protected override async FTask Run(Scene scene, M2G_SyncTransform message)
        {
            var session = scene.GetEntity<Session>(message.ClientID);
            if (session != null)
            {
                session.Send(new G2C_SyncTransform()
                {
                    NetworkObjectID=message.NetworkObjectID,
                    Transform=message.Transform,
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
