using ProtoBuf;
using ProtoBuf.Serializers;

namespace Fantasy.Serialize
{
    /// <summary>
    /// 空消息序列化器 - 用于没有字段的消息类型（如心跳、Ping等）
    /// 序列化结果为 0 字节，配合 BufferPacketParser 的空消息处理逻辑使用
    /// </summary>
    /// <typeparam name="T">消息类型，必须有无参构造函数</typeparam>
    public sealed class EmptyMessageSerializer<T> : ISerializer<T> where T : class, new()
    {
        /// <summary>
        /// 序列化器特性：字符串类型的消息
        /// </summary>
        SerializerFeatures ISerializer<T>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

        /// <summary>
        /// 反序列化空消息：跳过所有字段（如果有的话）
        /// </summary>
        T ISerializer<T>.Read(ref ProtoReader.State state, T value)
        {
            // 循环读取并跳过所有字段（正常情况下空消息不应该有字段）
            while (state.ReadFieldHeader() > 0)
            {
                state.SkipField();
            }
            return value ?? new T();
        }

        /// <summary>
        /// 序列化空消息：不写入任何数据（0 字节）
        /// </summary>
        void ISerializer<T>.Write(ref ProtoWriter.State state, T value)
        {
            // 空消息：不写入任何数据
            // BufferPacketParser 会检测到 packetBodyCount == 0 并将其设置为 -1
        }
    }
}
