using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    internal class WaitingRoom :Entity,IComparable<WaitingRoom>
    {
        List<long> waitingClient=new List<long>();
        int roomCount = 2;
        public long roomId;
        public int RoomCount { get => roomCount; set => roomCount = value; }
        public List<long> WaitingClient { get => waitingClient; }

        public bool TestLink()
        {
            for (int i = waitingClient.Count-1; i >=0; i--)
            {
                var session= this.Scene.GetEntity<Session>(waitingClient[i]);
                if (session == null || session.IsDisposed)
                {
                    waitingClient.RemoveAt(i);
                    return false;
                }
            }
            return true;
        }
        public bool IsFull() => waitingClient.Count == roomCount;

        public int CompareTo(WaitingRoom? other)
        {
            if (other == null) return -1;
            if(other == this) return 0;
            return (other.RoomCount - other.WaitingClient.Count).CompareTo(roomCount - waitingClient.Count);
        }
    }
}
