using System;
using System.IO;
using Fantasy.DataStructure;
using Fantasy.Helper;

namespace Fantasy.Core.Network
{
    public abstract class APacketParser : IDisposable
    {
        protected bool IsDisposed { get; private set; }

        public static APacketParser CreatePacketParser(NetworkTarget networkTarget)
        {
            switch (networkTarget)
            {
                case NetworkTarget.Inner:
                {
#if FANTASY_SERVER
                    return new InnerPacketParser();
                    #else
                    return null;
#endif
                }
                case NetworkTarget.Outer:
                {
                    return new OuterPacketParser();
                }
                default:
                {
                    throw new NotSupportedException($"PacketParserHelper Create NotSupport {networkTarget}");
                }
            }
        }

        public abstract bool UnPack(CircularBuffer buffer, out APackInfo packInfo);
        public abstract APackInfo UnPack(MemoryStream memoryStream);

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}