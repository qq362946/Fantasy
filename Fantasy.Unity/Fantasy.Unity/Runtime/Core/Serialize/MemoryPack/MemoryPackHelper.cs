using System;
using System.Buffers;
using MemoryPack;
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy
{
    public static class MemoryPackHelper
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(buffer.GetSpan());
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static object Deserialize(Type type, byte[] bytes)
        {
            var @object = MemoryPackSerializer.Deserialize(type,bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = MemoryPackSerializer.Deserialize(type, buffer.GetSpan());
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var @object = MemoryPackSerializer.Deserialize<T>(new ReadOnlySpan<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var @object = MemoryPackSerializer.Deserialize(type, new ReadOnlySpan<byte>(bytes, index, count));
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }

        public static void Serialize<T>(T @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            MemoryPackSerializer.Serialize<T, MemoryStreamBuffer>(buffer, @object);
        }

        public static void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            MemoryPackSerializer.Serialize(@object.GetType(), buffer, @object);
        }

        public static void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            MemoryPackSerializer.Serialize(type, buffer, @object);
        }

        public static byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            return MemoryPackSerializer.Serialize(@object.GetType(), @object);
        }

        public static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            return MemoryPackSerializer.Serialize<T>(@object);
        }

        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}