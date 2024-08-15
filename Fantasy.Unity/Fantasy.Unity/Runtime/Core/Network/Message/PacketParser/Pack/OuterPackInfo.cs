// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
using System.IO;
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

        public override MemoryStream RentMemoryStream(int size = 0)
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
            }

            MemoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
            var @object = ProtoBuffHelper.FromStream(messageType, MemoryStream);
            MemoryStream.Seek(0, SeekOrigin.Begin);
            return @object;
        }
    }
}