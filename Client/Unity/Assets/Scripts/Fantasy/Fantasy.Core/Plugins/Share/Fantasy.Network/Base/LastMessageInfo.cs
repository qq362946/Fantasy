using System;
using System.IO;

namespace Fantasy.Core.Network
{
    public class LastMessageInfo : IDisposable
    {
        public object Message;
        public MemoryStream MemoryStream;

        public void Dispose()
        {
            Message = null;
            MemoryStream = null;
        }
    }
}