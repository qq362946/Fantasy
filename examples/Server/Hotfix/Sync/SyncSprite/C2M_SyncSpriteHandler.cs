using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public class C2M_SyncSpriteHandler : Addressable<Unit, C2M_SyncSprite>
    {
        protected override async FTask Run(Unit entity, C2M_SyncSprite message)
        {
            void SetMessage(M2G_SyncSprite t, long id)
            {
                t.ClientID = id;
                t.NetworkObjectID = message.NetworkObjectID;
                t.SpriteName = message.SpriteName;
            }
            var room = entity.Scene.GetComponent<LogicMgr>().GetRoomById(entity.RoomID);
            room.M_Broadcast<M2G_SyncSprite>(SetMessage,entity.ClientID);


            await FTask.CompletedTask;
        }

       
    }
}
