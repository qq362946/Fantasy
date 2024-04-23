using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Fantasy
{
    /// <summary>
    /// 提供ProtoBuf序列化和反序列化的帮助方法类。
    /// </summary>
    public static class ProtoBuffHelper
    {
#if FANTASY_NET
        /// <summary>
        /// 初始化 ProtoBuffHelper。
        /// </summary>
        public static void Initialize()
        {
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AllowParseableTypes = true;
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AutoCompile = true;
            RuntimeTypeModel.Default.UseImplicitZeroDefaults = true;
            RuntimeTypeModel.Default.InferTagFromNameDefault = true;
            
            foreach (var type in AssemblySystem.ForEach(typeof(IProto)))
            {
                RuntimeTypeModel.Default.Add(type, true);
            }
            
            RuntimeTypeModel.Default.CompileInPlace();
        }
#endif
        /// <summary>
        /// 从 Span/byte 中反序列化对象。
        /// </summary>
        /// <param name="type">要反序列化的对象类型。</param>
        /// <param name="span">要反序列化的字节数据。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static object FromSpan(Type type, Span<byte> span)
        {
#if FANTASY_UNITY
            using var recyclableMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            recyclableMemoryStream.Write(span);
            recyclableMemoryStream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize(type, recyclableMemoryStream);
#else
            return RuntimeTypeModel.Default.Deserialize(type, span);
#endif
        }

        /// <summary>
        /// 从 Memory/byte 中反序列化对象。
        /// </summary>
        /// <param name="type">要反序列化的对象类型。</param>
        /// <param name="memory">要反序列化的内存数据。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static object FromMemory(Type type, Memory<byte> memory)
        {
#if FANTASY_UNITY
            using var recyclableMemoryStream = MemoryStreamHelper.GetRecyclableMemoryStream();
            recyclableMemoryStream.Write(memory.Span);
            recyclableMemoryStream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize(type, recyclableMemoryStream);
#else
            return RuntimeTypeModel.Default.Deserialize(type, memory);
#endif
        }

        /// <summary>
        /// 从指定的字节数组中的指定范围反序列化对象。
        /// </summary>
        /// <param name="type">要反序列化的对象类型。</param>
        /// <param name="bytes">包含对象序列化数据的字节数组。</param>
        /// <param name="index">要反序列化数据的起始索引。</param>
        /// <param name="count">要反序列化的字节数据长度。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static object FromBytes(Type type, byte[] bytes, int index, int count)
        {
#if FANTASY_UNITY
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
            stream.Write(bytes, index, count);
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize(type, stream);
#else
            var memory = new Memory<byte>(bytes, index, count);
            return RuntimeTypeModel.Default.Deserialize(type, memory);
#endif
        }

        /// <summary>
        /// 从字节数组中反序列化对象。
        /// </summary>
        /// <typeparam name="T">要反序列化的对象类型。</typeparam>
        /// <param name="bytes">包含对象序列化数据的字节数组。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static T FromBytes<T>(byte[] bytes)
        {
#if FANTASY_UNITY
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize<T>(stream);
#else
            return RuntimeTypeModel.Default.Deserialize<T>(new Span<byte>(bytes));
#endif
        }

        /// <summary>
        /// 从指定的字节数组中的指定范围反序列化对象。
        /// </summary>
        /// <typeparam name="T">要反序列化的对象类型。</typeparam>
        /// <param name="bytes">包含对象序列化数据的字节数组。</param>
        /// <param name="index">要反序列化数据的起始索引。</param>
        /// <param name="count">要反序列化的字节数据长度。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static T FromBytes<T>(byte[] bytes, int index, int count)
        {
#if FANTASY_UNITY
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize<T>(stream);
#else
            return RuntimeTypeModel.Default.Deserialize<T>(new Span<byte>(bytes, index, count));
#endif
        }

        /// <summary>
        /// 将对象序列化为字节数组。
        /// </summary>
        /// <param name="message">要序列化的对象。</param>
        /// <returns>包含序列化数据的字节数组。</returns>
        public static byte[] ToBytes(object message)
        {
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
            RuntimeTypeModel.Default.Serialize(stream, message);
            return stream.ToArray();
        }

        /// <summary>
        /// 将对象序列化到指定的内存中。
        /// </summary>
        /// <param name="message">要序列化的对象。</param>
        /// <param name="memory">目标内存。</param>
        public static void ToMemory(object message, Memory<byte> memory)
        {
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
            RuntimeTypeModel.Default.Serialize(stream, message);
            stream.GetBuffer().AsMemory().CopyTo(memory);
        }

        /// <summary>
        /// 将对象序列化到指定的流中。
        /// </summary>
        /// <param name="message">要序列化的对象。</param>
        /// <param name="stream">目标流。</param>
        public static void ToStream(object message, MemoryStream stream)
        {
            RuntimeTypeModel.Default.Serialize(stream, message);
        }

        /// <summary>
        /// 从指定的流中反序列化对象。
        /// </summary>
        /// <param name="type">要反序列化的对象类型。</param>
        /// <param name="stream">包含对象序列化数据的流。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static object FromStream(Type type, MemoryStream stream)
        {
#if FANTASY_UNITY
            return Serializer.Deserialize(type, stream);
#else
            return RuntimeTypeModel.Default.Deserialize(type, stream);
#endif
        }

        /// <summary>
        /// 从指定的流中反序列化对象。
        /// </summary>
        /// <typeparam name="T">要反序列化的对象类型。</typeparam>
        /// <param name="stream">包含对象序列化数据的流。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static T FromStream<T>(MemoryStream stream)
        {
#if FANTASY_UNITY
            return (T)Serializer.Deserialize(typeof(T), stream);
#else
            return RuntimeTypeModel.Default.Deserialize<T>(stream);
#endif
        }

        /// <summary>
        /// 克隆一个对象，通过序列化和反序列化实现深度复制。
        /// </summary>
        /// <typeparam name="T">要克隆的对象类型。</typeparam>
        /// <param name="t">要克隆的对象。</param>
        /// <returns>克隆后的新对象。</returns>
        public static T Clone<T>(T t)
        {
            using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
#if FANTASY_UNITY
            Serializer.Serialize(stream, t);
            return Serializer.Deserialize<T>(stream);
#else
            RuntimeTypeModel.Default.Serialize(stream, t);
            return RuntimeTypeModel.Default.Deserialize<T>(stream);
#endif
        }
    }
}