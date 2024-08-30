using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class C2M_DeleteNetworkObjHandler : Addressable<Unit, C2M_DeleteNetworkObj>
    {
        protected override async FTask Run(Unit unit, C2M_DeleteNetworkObj message)
        {

            var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
            var logMgr = unit.Scene.GetComponent<LogicMgr>();
            var ge = unit.GetGameEntity(message.NetworkObjectID);
            if (ge != null)
            {
                var room = logMgr.GetRoomById(unit.RoomID);
                if (room != null)
                {
                    unit.RemoveGameEntity(ge);
                    var clients = room.AllClientID();
                    foreach (var v in clients)
                    {
                        if (v == unit.ClientID) continue;
                        unit.Scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new M2G_DeleteNetworkObj()
                        {
                            ClientID = v,
                            NetworkObjectID = message.NetworkObjectID
                        });
                    }
                }
    
            }
       
            await FTask.CompletedTask;
        }
    }

}
