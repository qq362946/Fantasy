using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class M2G_HurtHandler : Route<Scene, M2G_Hurt>
    {
        protected override async FTask Run(Scene entity, M2G_Hurt message)
        {
            SendUtil.SendClient<G2C_Hurt>(entity,message.ClientID,new G2C_Hurt() { NetworkObjectID= message.NetworkObjectID,Value=message.Value });
            await FTask.CompletedTask;
        }
    }
}
