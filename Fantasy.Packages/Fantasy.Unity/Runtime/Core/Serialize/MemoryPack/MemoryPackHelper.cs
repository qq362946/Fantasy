using System;
using System.Buffers;
using Fantasy.Async;
using MemoryPack;

namespace Fantasy.Serialize
{
    /// <summary>
    /// MemoryPack 序列化实现，提供高性能的二进制序列化/反序列化功能。
    /// 使用 Cysharp.MemoryPack 库，支持零编码极致性能的序列化。
    /// </summary>
    public sealed class MemoryPackHelper : ISerialize
    {
        /// <inheritdoc/>
        public string SerializeName { get; } = "MemoryPack";

        /// <summary>
        /// 初始化 MemoryPack 序列化器。
        /// </summary>
        /// <returns>初始化后的 MemoryPackHelper 实例</returns>
        internal async FTask<MemoryPackHelper> Initialize()
        {
            await FTask.CompletedTask;
            return this;
        }

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes)
        {
            return MemoryPackSerializer.Deserialize<T>(bytes)!;
        }

        /// <inheritdoc/>
        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var span = new ReadOnlySpan<byte>(buffer.GetBuffer(), (int)buffer.Position,
                (int)(buffer.Length - buffer.Position));
            return MemoryPackSerializer.Deserialize<T>(span)!;
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, byte[] bytes)
        {
            return MemoryPackSerializer.Deserialize(type, bytes)!;
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var span = new ReadOnlySpan<byte>(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
            return MemoryPackSerializer.Deserialize(type, span)!;
        }

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var span = new ReadOnlySpan<byte>(bytes, index, count);
            return MemoryPackSerializer.Deserialize<T>(span)!;
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var span = new ReadOnlySpan<byte>(bytes, index, count);
            return MemoryPackSerializer.Deserialize(type, span)!;
        }

        /// <inheritdoc/>
        public byte[] Serialize(object obj)
        {
            return MemoryPackSerializer.Serialize(obj.GetType(), obj);
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T @object)
        {
            return MemoryPackSerializer.Serialize(@object);
        }

        /// <inheritdoc/>
        public void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            MemoryPackSerializer.Serialize(buffer, @object);
        }

        /// <inheritdoc/>
        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            MemoryPackSerializer.Serialize(@object.GetType(), buffer, @object);
        }

        /// <inheritdoc/>
        public void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            MemoryPackSerializer.Serialize(type, buffer, @object);
        }

        /// <inheritdoc/>
        public T Clone<T>(T t)
        {
            var bytes = MemoryPackSerializer.Serialize(t);
            return MemoryPackSerializer.Deserialize<T>(bytes)!;
        }
    }
}