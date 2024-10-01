using System;
using System.Buffers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Serialize
{
    public interface ISerialize
    {
        /// <summary>
        /// 序列化器的名字，用于在协议里指定用什么协议序列化使用
        /// </summary>
        string SerializeName { get; }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(byte[] bytes);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(MemoryStreamBuffer buffer);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        object Deserialize(Type type, byte[] bytes);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        object Deserialize(Type type, MemoryStreamBuffer buffer);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(byte[] bytes, int index, int count);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        object Deserialize(Type type, byte[] bytes, int index, int count);
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        void Serialize<T>(T @object, IBufferWriter<byte> buffer);
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        void Serialize(object @object, IBufferWriter<byte> buffer);
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        void Serialize(Type type, object @object, IBufferWriter<byte> buffer);
        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Clone<T>(T t);
    }
}