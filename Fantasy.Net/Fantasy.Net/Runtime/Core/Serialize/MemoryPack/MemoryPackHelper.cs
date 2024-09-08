using System;
using System.Buffers;
using MemoryPack;
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Serialize
{
    /// <summary>
    /// MemoryPack帮助类
    /// </summary>
    public static class MemoryPackHelper
    {
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(buffer.GetSpan());
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, byte[] bytes)
        {
            var @object = MemoryPackSerializer.Deserialize(type,bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = MemoryPackSerializer.Deserialize(type, buffer.GetSpan());
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(new ReadOnlySpan<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var @object = MemoryPackSerializer.Deserialize(type, new ReadOnlySpan<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        public static void Serialize<T>(T @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            MemoryPackSerializer.Serialize<T, MemoryStreamBuffer>(buffer, @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public static void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            MemoryPackSerializer.Serialize(@object.GetType(), buffer, @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public static void Serialize(Type type, object @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            MemoryPackSerializer.Serialize(type, buffer, @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            return MemoryPackSerializer.Serialize(@object.GetType(), @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            return MemoryPackSerializer.Serialize<T>(@object);
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}