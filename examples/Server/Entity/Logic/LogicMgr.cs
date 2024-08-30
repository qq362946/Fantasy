using Fantasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;


public class LogicMgr : Entity
{

    List<Room> roomList = new List<Room>();

    public void EnterRoom(Unit unit)
    {
        var room = roomList.Find(u => u.roomId == unit.RoomID);
        if(room == null)
        {
            room = Room.Create<Room>(this.Scene,false,false);
            room.roomId = unit.RoomID;
            roomList.Add(room);
        }
        room.AddUnit(unit);
    }
    public void RemoveRoom(Room room)
    {
        roomList.Remove(room);
    }
    public Room GetRoomById(long roomId) => roomList.Find(r => r.roomId == roomId);
    public Unit GetUnitByClientId(long clientID)
    {
        for(int i=0;i<roomList.Count;i++)
        {
            var res = roomList[i].GetUnitByClientId(clientID);
            if(res!=null) return res;
        }
        return null;
    }
    public void RemoveUnit(Unit unit)
    {
        var room = GetRoomById(unit.RoomID);
        if(room == null) return;
        room.RemoveUnit(unit.ClientID);
    }
    public void RemoveUnit(long clientID)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            var res = roomList[i].GetUnitByClientId(clientID);
            if (res != null)
            {
                roomList[i].RemoveUnit(clientID);
            }
        }
    }
}

