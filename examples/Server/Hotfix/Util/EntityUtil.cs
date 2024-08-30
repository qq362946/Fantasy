using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public static class EntityUtil
    {
        public static void HurtBody(this GameBody body,long value)
        {
            body.hp -= value;
            if (body.hp <= 0)
            {
                var ge = (GameEntity)body.Parent;
                var lom = ge.Scene.GetComponent<LogicMgr>();
                var unit= lom.GetUnitByClientId(ge.clientID);
                var room = lom.GetRoomById(unit.RoomID);
                unit.RemoveGameEntity(ge);
                room.M_Broadcast<M2G_DeleteNetworkObj>((t, l) => { t.NetworkObjectID = ge.networkObjectID;t.ClientID = l; });
                ge.Dispose();
                var otherUnits =room.GetAllUnits();
                //bool isFinish = false;
                int livePlayerCount = 0;
                for (int i = 0;i<otherUnits.Length;i++)
                {
                    //if (otherUnits[i] == unit) continue;
                    if (otherUnits[i].EntitesCount > 0)
                    {
                        livePlayerCount++;
                    }
                }
                if (livePlayerCount <=1)
                {
                    //游戏结束
                    room.GameOver();
                }
            }
        }

        public static void GameOver(this Room room)
        {
            var otherUnits = room.GetAllUnits();
            Unit liveUnit = null;
            for (int i = 0; i < otherUnits.Length; i++)
            {
                //if (otherUnits[i] == unit) continue;
                if (otherUnits[i].EntitesCount > 0)
                {
                    liveUnit=otherUnits[i];
                    break;
                }
            }
            room.M_Broadcast<M2G_GameOver>((m, l) => { m.ClientID = l; m.WinnerClientID = liveUnit.ClientID; });



            //room.Scene.GetComponent<LogicMgr>().RemoveRoom(room);
            //room.Dispose();
        }

    }
}
