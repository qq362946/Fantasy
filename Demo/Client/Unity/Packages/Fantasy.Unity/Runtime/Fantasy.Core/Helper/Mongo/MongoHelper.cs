#if FANTASY_NET
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Unity.Mathematics;

namespace Fantasy.Helper;

public sealed class MongoHelper : Singleton<MongoHelper>
{
    private readonly HashSet<int> _registerCount = new HashSet<int>(3);

    static MongoHelper()
    {
        // 自动注册IgnoreExtraElements
        var conventionPack = new ConventionPack {new IgnoreExtraElementsConvention(true)};
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);
        BsonSerializer.TryRegisterSerializer(typeof(float2), new StructBsonSerialize<float2>());
        BsonSerializer.TryRegisterSerializer(typeof(float3), new StructBsonSerialize<float3>());
        BsonSerializer.TryRegisterSerializer(typeof(float4), new StructBsonSerialize<float4>());
        BsonSerializer.TryRegisterSerializer(typeof(quaternion), new StructBsonSerialize<quaternion>());
        BsonSerializer.RegisterSerializer(new ObjectSerializer(x => true));
    }

    protected override void OnLoad(int assemblyName)
    {
        if (_registerCount.Count == 3)
        {
            return;
        }

        _registerCount.Add(assemblyName);

        Task.Run(() =>
        {
            foreach (var type in AssemblyManager.ForEach(assemblyName))
            {
                if (type.IsInterface || type.IsAbstract || !typeof(Entity).IsAssignableFrom(type))
                {
                    continue;
                }
                
                BsonClassMap.LookupClassMap(type);
            }
        });
    }

    public object Deserialize(Span<byte> span, Type type)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize(stream, type);
    }

    public object Deserialize(Memory<byte> memory, Type type)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(memory.Span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize(stream, type);
    }
    
    public object Deserialize<T>(Span<byte> span)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize<T>(stream);
    }

    public object Deserialize<T>(Memory<byte> memory)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize<T>(stream);
    }

    public T Deserialize<T>(byte[] bytes)
    {
        return BsonSerializer.Deserialize<T>(bytes);
    }

    public object Deserialize(byte[] bytes, Type type)
    {
        return BsonSerializer.Deserialize(bytes, type);
    }
    
    public object Deserialize(byte[] bytes, string type)
    {
        return BsonSerializer.Deserialize(bytes, Type.GetType(type));
    }
    
    public T Deserialize<T>(Stream stream)
    {
        return BsonSerializer.Deserialize<T>(stream);
    }
    
    public object Deserialize(Stream stream, Type type)
    {
        return BsonSerializer.Deserialize(stream, type);
    }

    public object DeserializeFrom(Type type, MemoryStream stream)
    {
        return Deserialize(stream, type);
    }

    public T DeserializeFrom<T>(MemoryStream stream)
    {
        return Deserialize<T>(stream);
    }

    public T DeserializeFrom<T>(byte[] bytes, int index, int count)
    {
        return BsonSerializer.Deserialize<T>(bytes.AsMemory(index, count).ToArray());
    }
    
    public byte[] SerializeTo<T>(T t)
    {
        return t.ToBson();
    }

    public void SerializeTo<T>(T t, Memory<byte> memory)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
        {
            BsonSerializer.Serialize(writer, typeof(T), t);
        }

        memoryStream.GetBuffer().AsMemory().CopyTo(memory);
    }

    public void SerializeTo<T>(T t, MemoryStream stream)
    {
        using var writer = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults);
        BsonSerializer.Serialize(writer, typeof(T), t);
    }

    public T Clone<T>(T t)
    {
        return Deserialize<T>(t.ToBson());
    }
}
#endif
#if FANTASY_UNITY
using System;
using System.IO;
namespace Fantasy.Helper
{
    public sealed class MongoHelper : Singleton<MongoHelper>
    {
        public object DeserializeFrom(Type type, MemoryStream stream)
        {
            return null;
        }
    }
}
#endif
