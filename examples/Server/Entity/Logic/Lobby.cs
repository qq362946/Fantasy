using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public class Lobby :Entity
    {
        List<WaitingRoom> waitingRooms = new List<WaitingRoom>();
        long roomId = 0;
        public long RoomId { get { return roomId; } set { if (value == long.MaxValue) value = 0; roomId = value; } }

        public void RemoveWaitingClient(long id)
        {
            for (int i = waitingRooms.Count-1; i >=0; i--)
            {
                if (waitingRooms[i].WaitingClient.Contains(id))
                {
                    waitingRooms[i].WaitingClient.Remove(id);
                    if (waitingRooms[i].WaitingClient.Count==0)
                    {
                        var r = waitingRooms[i];
                        waitingRooms.RemoveAt(i);
                        r.Dispose();
                    }    
                }
            }

        }

        public void AddWaitingClient(long id,int maxCount)
        {
            waitingRooms.Sort();
            WaitingRoom selectRoom = null;
            for (int i = 0; i < waitingRooms.Count; i++)
            {
                if (waitingRooms[i].RoomCount == maxCount)
                {
                    waitingRooms[i].WaitingClient.Add(id);
                    selectRoom = waitingRooms[i];
                    break;
                }
            }
            if (selectRoom == null)
            {
                selectRoom = WaitingRoom.Create<WaitingRoom>(this.Scene, true, false);
                selectRoom.roomId = RoomId++;
                selectRoom.RoomCount= maxCount;
                waitingRooms.Add(selectRoom);
                selectRoom.WaitingClient.Add(id);
            }
            if (selectRoom.IsFull())
            {
                if (selectRoom.TestLink())
                {
                    waitingRooms.Remove(selectRoom);
                    //开始游戏
                    Log.Debug("房间满了准备开了,房间号:" + selectRoom.roomId);
                    for(int i=0;i<selectRoom.WaitingClient.Count;i++) 
                    {
                        this.Scene.GetEntity<Session>(selectRoom.WaitingClient[i]).Send(new G2C_LobbyFinishMessage()
                        {
                            roomID = selectRoom.roomId
                        }) ;
                    }
                }
            }
            else
            {
                Log.Debug($"{id} 加入等待,房间号:" + selectRoom.roomId);
            }
        }

    }
}
