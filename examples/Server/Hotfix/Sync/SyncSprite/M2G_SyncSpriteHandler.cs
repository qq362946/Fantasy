using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_SyncSpriteHandler : Route<Scene, M2G_SyncSprite>
    {
        protected override async FTask Run(Scene entity, M2G_SyncSprite message)
        {
            SendUtil.SendClient<G2C_SyncSprite>(entity,message.ClientID,new G2C_SyncSprite() { NetworkObjectID= message.NetworkObjectID,SpriteName=message.SpriteName });
            await FTask.CompletedTask;
        }
    }
}
