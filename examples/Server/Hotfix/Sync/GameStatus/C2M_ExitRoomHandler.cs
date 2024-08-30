using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public class C2M_ExitRoomHandler : Addressable<Unit, C2M_ExitRoom>
    {
        protected override async FTask Run(Unit entity, C2M_ExitRoom message)
        {
            void SetMessage(M2G_ExitRoom t, long id)
            {
                t.ClientID = id;
            }
            var room = entity.Scene.GetComponent<LogicMgr>().GetRoomById(entity.RoomID);
            room.M_Broadcast<M2G_ExitRoom>(SetMessage, entity.ClientID);

            entity.Dispose();//死的时候自己会退出房间

            await FTask.CompletedTask;
        }


    }
}
