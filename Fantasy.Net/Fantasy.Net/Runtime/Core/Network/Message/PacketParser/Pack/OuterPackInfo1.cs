// using System;
// using System.Buffers;
// using System.IO;
// // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
// #pragma warning disable CS8602 // Dereference of a possibly null reference.
// #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
//
// namespace Fantasy
// {
//     public sealed class OuterPackInfo : APackInfo
//     {
//         public override void Dispose()
//         {
//             var network = Network;
//             base.Dispose();
//             network.ReturnOuterPackInfo(this);
//         }
//
//         public static OuterPackInfo Create(ANetwork network)
//         {
//             var outerPackInfo = network.RentOuterPackInfo();
//             outerPackInfo.Network = network;
//             outerPackInfo.IsDisposed = false;
//             return outerPackInfo;
//         }
//
//         public override MemoryStream RentMemoryStream(int size = 0)
//         {
//             if (MemoryStream == null)
//             {
//                 MemoryStream = Network.RentMemoryStream(size);
//             }
//
//             return MemoryStream;
//         }
//
//         /// <summary>
//         /// 将消息数据从内存反序列化为指定的消息类型实例。
//         /// </summary>
//         /// <param name="messageType">目标消息类型。</param>
//         /// <returns>反序列化后的消息类型实例。</returns>
//         public override object Deserialize(Type messageType)
//         {
//             if (MemoryStream == null)
//             {
//                 Log.Debug("Deserialize MemoryStream is null");
//             }
//             
//             MemoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
//             var @object = ProtoBuffHelper.FromStream(messageType, MemoryStream);
//             MemoryStream.Seek(0, SeekOrigin.Begin);
//             return @object;
//         }
//     }
//
//     /// <summary>
//     /// 用于解析外部网络消息的数据包解析器。
//     /// </summary>
//     public sealed class OuterPacketParser : APacketParser
//     {
//         private uint _rpcId;
//         private uint _protocolCode;
//         private long _routeTypeCode;
//         private int _messagePacketLength;
//         private bool _isUnPackHead = true;
//         private readonly byte[] _packRpcId = new byte[4];
//         private readonly byte[] _packOpCode = new byte[4];
//         private readonly byte[] _packPacketBodyCount = new byte[4];
//         private readonly byte[] _packRouteTypeOpCode = new byte[8];
//         private readonly byte[] _messageHead = new byte[Packet.OuterPacketHeadLength];
//
//         public override bool UnPack(ref ReadOnlyMemory<byte> buffer, out APackInfo packInfo)
//         {
//             throw new NotImplementedException();
//         }
//
//         /// <summary>
//         /// 用于解析外部网络消息的数据包解析器。
//         /// </summary>
//         /// <param name="buffer">循环缓冲区，用于存储接收到的数据。</param>
//         /// <param name="packInfo">解析后的数据包信息。</param>
//         /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
//         public override bool UnPack(CircularBuffer buffer, out APackInfo packInfo)
//         {
//             packInfo = null;
//
//             while (!IsDisposed)
//             {
//                 if (_isUnPackHead)
//                 {
//                     if (buffer.Length < Packet.OuterPacketHeadLength)
//                     {
//                         return false;
//                     }
//
//                     _ = buffer.Read(_messageHead, 0, Packet.OuterPacketHeadLength);
//                     _messagePacketLength = BitConverter.ToInt32(_messageHead, 0);
// #if FANTASY_NET
//                     if (_messagePacketLength > Packet.PacketBodyMaxLength)
//                     {
//                         throw new ScanException($"The received information exceeds the maximum limit = {_messagePacketLength}");
//                     }
// #endif
//                     _protocolCode = BitConverter.ToUInt32(_messageHead, Packet.PacketLength);
//                     _rpcId = BitConverter.ToUInt32(_messageHead, Packet.OuterPacketRpcIdLocation);
//                     _routeTypeCode = BitConverter.ToUInt16(_messageHead, Packet.OuterPacketRouteTypeOpCodeLocation);
//                     _isUnPackHead = false;
//                 }
//
//                 try
//                 {
//                     if (_messagePacketLength < 0 || buffer.Length < _messagePacketLength)
//                     {
//                         return false;
//                     }
//                     
//                     _isUnPackHead = true;
//                     packInfo = OuterPackInfo.Create(Network);
//                     var memoryStream = packInfo.RentMemoryStream(_messagePacketLength);
//                     buffer.Read(memoryStream, _messagePacketLength);
//                     packInfo.RpcId = _rpcId;
//                     packInfo.ProtocolCode = _protocolCode;
//                     packInfo.RouteTypeCode = _routeTypeCode;
//                     packInfo.MessagePacketLength = _messagePacketLength;
//                     memoryStream.Seek(0, SeekOrigin.Begin);
//                     return true;
//                 }
//                 catch (Exception e)
//                 {
//                     packInfo?.Dispose();
//                     Log.Error(e);
//                     return false;
//                 }
//             }
//
//             return false;
//         }
//
//         /// <summary>
//         /// 从内存中解析数据包。
//         /// </summary>
//         /// <param name="buffer">需要解包的buffer。</param>
//         /// <param name="count">解包的总长度。</param>
//         /// <param name="packInfo">解析得到的数据包信息。</param>
//         /// <returns>如果成功解析数据包，则返回 true；否则返回 false。</returns>
//         public override bool UnPack(byte[] buffer, ref int count, out APackInfo packInfo)
//         {
//             packInfo = null;
//
//             try
//             {
//                 if (count < Packet.OuterPacketHeadLength)
//                 {
//                     return false;
//                 }
//
//                 _messagePacketLength = BitConverter.ToInt32(buffer);
// #if FANTASY_NET
//                 if (_messagePacketLength > Packet.PacketBodyMaxLength)
//                 {
//                     throw new ScanException($"The received information exceeds the maximum limit = {_messagePacketLength}");
//                 }
// #endif
//                 if (_messagePacketLength < 0 || count < _messagePacketLength)
//                 {
//                     return false;
//                 }
//
//                 packInfo = OuterPackInfo.Create(Network);
//                 var newMemoryStream = packInfo.RentMemoryStream(_messagePacketLength);
//                 newMemoryStream.Write(buffer, 0, _messagePacketLength);
//                 packInfo.MessagePacketLength = _messagePacketLength;
//                 packInfo.ProtocolCode = BitConverter.ToUInt32(buffer, Packet.PacketLength);
//                 packInfo.RpcId = BitConverter.ToUInt32(buffer, Packet.OuterPacketRpcIdLocation);
//                 packInfo.RouteTypeCode = BitConverter.ToUInt16(buffer, Packet.OuterPacketRouteTypeOpCodeLocation);
//                 newMemoryStream.Seek(0, SeekOrigin.Begin);
//                 return true;
//             }
//             catch (Exception e)
//             {
//                 packInfo?.Dispose();
//                 Log.Error(e);
//                 return false;
//             }
//         }
//
//         public override MemoryStream Pack(ref uint rpcId, ref long routeTypeOpCode, ref long routeId, MemoryStream memoryStream, object message)
//         {
//             return memoryStream == null
//                 ? Pack(rpcId, routeTypeOpCode, message)
//                 : Pack(rpcId, routeTypeOpCode, memoryStream);
//         }
//
//         /// <summary>
//         /// 封装数据包。
//         /// </summary>
//         /// <param name="rpcId">RPC标识。</param>
//         /// <param name="routeTypeOpCode">路由类型和操作码。</param>
//         /// <param name="memoryStream">要封装的内存流。</param>
//         /// <returns>封装后的内存流。</returns>
//         private MemoryStream Pack(uint rpcId, long routeTypeOpCode, MemoryStream memoryStream)
//         {
//             rpcId.GetBytes(_packRpcId);
//             routeTypeOpCode.GetBytes(_packRouteTypeOpCode);
//
//             memoryStream.Seek(Packet.OuterPacketRpcIdLocation, SeekOrigin.Begin);
//             memoryStream.Write(_packRpcId);
//             memoryStream.Write(_packRouteTypeOpCode);
//             memoryStream.Seek(0, SeekOrigin.Begin);
//             return memoryStream;
//         }
//
//         /// <summary>
//         /// 封装数据包。
//         /// </summary>
//         /// <param name="rpcId">RPC标识。</param>
//         /// <param name="routeTypeOpCode">路由类型和操作码。</param>
//         /// <param name="message">要封装的消息对象。</param>
//         /// <returns>封装后的内存流。</returns>
//         public MemoryStream Pack(uint rpcId, long routeTypeOpCode, object message)
//         {
//             var opCode = Opcode.PingRequest;
//             var packetBodyCount = 0;
//             var memoryStream = Network.RentMemoryStream();
//             memoryStream.Seek(Packet.OuterPacketHeadLength, SeekOrigin.Begin);
//
//             if (message != null)
//             {
//                 ProtoBuffHelper.ToStream(message, memoryStream);
//                 opCode = MessageDispatcherComponent.GetOpCode(message.GetType());
//                 packetBodyCount = (int)(memoryStream.Position - Packet.OuterPacketHeadLength);
//                 // 如果消息是对象池的需要执行Dispose
//                 if (message is IPoolMessage iPoolMessage)
//                 {
//                     iPoolMessage.Dispose();
//                 }
//             }
//
//             if (packetBodyCount > Packet.PacketBodyMaxLength)
//             {
//                 throw new Exception($"Message content exceeds {Packet.PacketBodyMaxLength} bytes");
//             }
//
//             rpcId.GetBytes(_packRpcId);
//             opCode.GetBytes(_packOpCode);
//             packetBodyCount.GetBytes(_packPacketBodyCount);
//             routeTypeOpCode.GetBytes(_packRouteTypeOpCode);
//             
//             memoryStream.Seek(0, SeekOrigin.Begin);
//             memoryStream.Write(_packPacketBodyCount);
//             memoryStream.Write(_packOpCode);
//             memoryStream.Write(_packRpcId);
//             memoryStream.Write(_packRouteTypeOpCode);
//             memoryStream.Seek(0, SeekOrigin.Begin);
//             
//             return memoryStream;
//         }
//
//         /// <summary>
//         /// 释放资源并重置状态。
//         /// </summary>
//         public override void Dispose()
//         {
//             _rpcId = 0;
//             _protocolCode = 0;
//             _routeTypeCode = 0;
//             _isUnPackHead = true;
//             _messagePacketLength = 0;
//             Array.Clear(_messageHead, 0, _messageHead.Length);
//             base.Dispose();
//         }
//     }
// }