using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_GameOverHandler : Route<Scene, M2G_GameOver>
    {
        protected override async FTask Run(Scene entity, M2G_GameOver message)
        {
            SendUtil.SendClient<G2C_GameOver>(entity,message.ClientID,new G2C_GameOver() { WinnerClientID=message.WinnerClientID });
            await FTask.CompletedTask;
        }
    }
}
