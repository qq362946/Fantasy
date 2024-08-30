using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.System
{
    internal class UnitDestroySystem : DestroySystem<Unit>
    {
        protected override void Destroy(Unit self)
        {
            //var logicMgr= self.Scene.GetComponent<LogicMgr>();
            //logicMgr.RemoveUnit(self.ClientID);
            ////Log.Debug("客户端销毁");
            var room = self.Scene.GetComponent<LogicMgr>().GetRoomById(self.RoomID);
            if(room != null)
            {
                room.RemoveUnit(self.ClientID);
            }
        }
    }
}
