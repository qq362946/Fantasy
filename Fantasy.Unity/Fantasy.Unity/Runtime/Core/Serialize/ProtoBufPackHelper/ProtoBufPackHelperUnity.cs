#if FANTASY_UNITY || FANTASY_CONSOLE
using System;
using System.Buffers;
using System.IO;
using Fantasy.Assembly;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Fantasy.Serialize
{
    /// <summary>
    /// ProtoBufP帮助类，Unity平台使用
    /// </summary>
    public sealed class ProtoBufPackHelper : ISerialize
    {
        /// <summary>
        /// 序列化器的名字
        /// </summary>
        public string SerializeName { get; } = "ProtoBuf";

        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public unsafe T Deserialize<T>(byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                using var stream = new UnmanagedMemoryStream(ptr, bytes.Length);
                var @object = ProtoBuf.Serializer.Deserialize<T>(stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = ProtoBuf.Serializer.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public unsafe object Deserialize(Type type, byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                using var stream = new UnmanagedMemoryStream(ptr, bytes.Length);
                var @object = ProtoBuf.Serializer.Deserialize(type, stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }

                return @object;
            }
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = ProtoBuf.Serializer.Deserialize(type, buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public unsafe T Deserialize<T>(byte[] bytes, int index, int count)
        {
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                var @object = ProtoBuf.Serializer.Deserialize<T>(stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public unsafe object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                var @object = ProtoBuf.Serializer.Deserialize(type, stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        public void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            RuntimeTypeModel.Default.Serialize((MemoryStream)buffer, @object);
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            RuntimeTypeModel.Default.Serialize((MemoryStream)buffer, @object);
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            RuntimeTypeModel.Default.Serialize((MemoryStream)buffer, @object);
        }
        private byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            using (var buffer = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize(buffer, @object);
                return buffer.ToArray();
            }
        }
        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}
#endif