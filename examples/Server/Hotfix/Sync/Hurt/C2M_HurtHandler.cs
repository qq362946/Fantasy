using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public class C2M_HurtHandler : Addressable<Unit, C2M_Hurt>
    {
        protected override async FTask Run(Unit entity, C2M_Hurt message)
        {
            void SetMessage(M2G_Hurt t, long id)
            {
                t.ClientID = id;
                t.NetworkObjectID = message.NetworkObjectID;
                t.Value = message.Value;
            }
            var body = entity?.GetGameEntity(message.NetworkObjectID)?.GetComponent<GameBody>();
            if (body!=null)
            {
                body.HurtBody(message.Value);
                var room = entity.Scene.GetComponent<LogicMgr>().GetRoomById(entity.RoomID);
                room.M_Broadcast<M2G_Hurt>(SetMessage, entity.ClientID);
            }
         


            await FTask.CompletedTask;
        }

       
    }
}
