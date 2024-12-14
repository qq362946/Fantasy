#if FANTASY_UNITY
using System;
using System.Buffers;
namespace Fantasy.Serialize
{
    public class BsonPackHelper : ISerialize
    {
        public string SerializeName { get; } = "Bson";
        public T Deserialize<T>(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(Type type, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(byte[] bytes, int index, int count)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            throw new NotImplementedException();
        }

        public void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public T Clone<T>(T t)
        {
            throw new NotImplementedException();
        }
    }
}
#endif