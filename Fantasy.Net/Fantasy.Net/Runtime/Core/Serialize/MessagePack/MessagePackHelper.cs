using System;
using System.Buffers;
using MessagePack;
// ReSharper disable CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy
{
    public static class MessagePackHelper
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }

        public static T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            return MessagePackSerializer.Deserialize<T>(buffer);
        }
    
        public static object Deserialize(Type type, byte[] bytes)
        {
            return MessagePackSerializer.Deserialize(type, bytes);
        }

        public static object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            return MessagePackSerializer.Deserialize(type, buffer);
        }

        public static T Deserialize<T>(byte[] bytes, int index, int count)
        {
            return MessagePackSerializer.Deserialize<T>(new Memory<byte>(bytes, index, count));
        }

        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            return MessagePackSerializer.Deserialize(type, new Memory<byte>(bytes, index, count));
        }

        public static void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            MessagePackSerializer.Serialize(buffer, @object);
        }
        
        public static void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            MessagePackSerializer.Serialize(@object.GetType(), buffer, @object);
        }
        
        public static void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            MessagePackSerializer.Serialize(type, buffer, @object);
        }

        public static byte[] Serialize(object @object)
        {
            return MessagePackSerializer.Serialize(@object.GetType(), @object);
        }

        public static byte[] Serialize<T>(T @object)
        {
            return MessagePackSerializer.Serialize<T>(@object);
        }

        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}

