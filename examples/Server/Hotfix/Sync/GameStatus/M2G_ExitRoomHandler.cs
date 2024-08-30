using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_ExitRoomHandler : Route<Scene, M2G_ExitRoom>
    {
        protected override async FTask Run(Scene entity, M2G_ExitRoom message)
        {

            SendUtil.SendClient(entity, message.ClientID, new G2C_ExitRoom()
            {
                ClientID = message.ExitClientId
            });

            await FTask.CompletedTask;
        }
    }
}
