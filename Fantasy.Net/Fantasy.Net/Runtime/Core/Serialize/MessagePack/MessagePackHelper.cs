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
            var @object = MessagePackSerializer.Deserialize<T>(bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = MessagePackSerializer.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
    
        public static object Deserialize(Type type, byte[] bytes)
        {
            var @object = MessagePackSerializer.Deserialize(type, bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = MessagePackSerializer.Deserialize(type, buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var @object = MessagePackSerializer.Deserialize<T>(new Memory<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var @object = MessagePackSerializer.Deserialize(type, new Memory<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            MessagePackSerializer.Serialize(buffer, @object);
        }
        
        public static void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            MessagePackSerializer.Serialize(@object.GetType(), buffer, @object);
        }
        
        public static void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            MessagePackSerializer.Serialize(type, buffer, @object);
        }

        public static byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            return MessagePackSerializer.Serialize(@object.GetType(), @object);
        }

        public static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            return MessagePackSerializer.Serialize<T>(@object);
        }

        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}

