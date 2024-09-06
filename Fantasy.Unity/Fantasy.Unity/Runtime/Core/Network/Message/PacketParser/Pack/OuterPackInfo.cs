// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
using System.IO;
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace Fantasy
{
    public sealed class OuterPackInfo : APackInfo
    {
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            var network = Network;
            base.Dispose();
            network.ReturnOuterPackInfo(this);
        }

        public static OuterPackInfo Create(ANetwork network)
        {
            var outerPackInfo = network.RentOuterPackInfo();
            outerPackInfo.Network = network;
            outerPackInfo.IsDisposed = false;
            return outerPackInfo;
        }

        public override MemoryStreamBuffer RentMemoryStream(int size = 0)
        {
            if (MemoryStream == null)
            {
                MemoryStream = Network.RentMemoryStream(size);
            }

            return MemoryStream;
        }

        /// <summary>
        /// 将消息数据从内存反序列化为指定的消息类型实例。
        /// </summary>
        /// <param name="messageType">目标消息类型。</param>
        /// <returns>反序列化后的消息类型实例。</returns>
        public override object Deserialize(Type messageType)
        {
            if (MemoryStream == null)
            {
                Log.Debug("Deserialize MemoryStream is null");
                return null;
            }

            MemoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            
            object obj = null;
            
            switch (OpCodeIdStruct.OpCodeProtocolType)
            {
                case OpCodeProtocolType.ProtoBuf:
                {
                    obj = ProtoBufPackHelper.Deserialize(messageType, MemoryStream);
                    break;
                }
                case OpCodeProtocolType.MemoryPack:
                {
                    obj = MemoryPackHelper.Deserialize(messageType, MemoryStream);
                    break;
                }
                default:
                {
                    MemoryStream.Seek(0, SeekOrigin.Begin);
                    Log.Error($"protocolCode:{ProtocolCode} Does not support processing protocol");
                    return null;
                }
            }
            
            MemoryStream.Seek(0, SeekOrigin.Begin);
            return obj;
        }
    }
}