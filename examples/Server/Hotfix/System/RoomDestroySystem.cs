using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy {
    internal class RoomDestroySystem : DestroySystem<Room>
    {
        protected override void Destroy(Room self)
        {
            var units= self.GetAllUnits();
            foreach(var unit in units)
            {
                if (unit == null || unit.IsDisposed) continue;
                unit.RoomID = -1;
                unit.Reset();
            }
           
        }
    }
}
