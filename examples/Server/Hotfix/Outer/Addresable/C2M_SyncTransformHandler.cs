using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Fantasy
{
    internal class C2M_SyncTransformHandler : Addressable<Unit, C2M_SyncTransform>
    {
        protected override async FTask Run(Unit unit, C2M_SyncTransform message)
        {
            var logmgr = unit.Scene.GetComponent<LogicMgr>();
            var ge= unit.GetGameEntity(message.NetworkObjectID);
            var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
            if (ge != null)
            {
                var room = logmgr.GetRoomById(unit.RoomID);
                if (room != null)
                {
                    ge.transformData = message.Transform;
                    var clientIds = room.AllClientID();
                    foreach (var v in clientIds)
                    {
                        if (v == unit.ClientID) continue;
                        unit.Scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new M2G_SyncTransform()
                        {
                            ClientID = v,
                            NetworkObjectID = ge.networkObjectID,
                            Transform = ge.transformData
                        });
                    }
                }
         
           
            }
            else
            {
                Log.Warning("试图同步一个不存在的东西");
            }

            await FTask.CompletedTask;
        }
    }
}
