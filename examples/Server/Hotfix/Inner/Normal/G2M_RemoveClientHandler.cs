using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;


namespace Fantasy
{
    internal class G2M_RemoveClientHandler : Route<Scene, G2M_RemoveClient>
    {
        protected override async FTask Run(Scene scene, G2M_RemoveClient message)
        {
            var logicMgr=scene.GetComponent<LogicMgr>();
            var unit = logicMgr.GetUnitByClientId(message.ClientID);
            if (unit != null)
            {
                var room = logicMgr.GetRoomById(unit.RoomID);
                if(room != null)
                {
                    Log.Debug($"移除玩家数据:,需要被通知移除单位数量:{unit.GetAllGameEntites().Count()}");
                    logicMgr.RemoveUnit(unit);
                    var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
                    foreach (var g in unit.GetAllGameEntites())
                    {
                        foreach (var v in room.AllClientID())
                        {
                            scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new M2G_DeleteNetworkObj() { ClientID = v, NetworkObjectID = g.networkObjectID });
                        }
                    }
                }
   
                Log.Debug($"玩家被移除:ClientID:{message.ClientID}");
            }
            await FTask.CompletedTask;
        }
    }
}