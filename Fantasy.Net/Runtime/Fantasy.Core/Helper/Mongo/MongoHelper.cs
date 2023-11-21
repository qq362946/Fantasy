#if FANTASY_NET
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Unity.Mathematics;

namespace Fantasy;

/// <summary>
/// 提供与MongoDB数据序列化和反序列化相关的帮助方法。
/// </summary>
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

    /// <summary>
    /// 在装载程序集时执行的方法，用于注册实体类型到BsonClassMap。
    /// </summary>
    /// <param name="assemblyName">要注册的程序集名称。</param>
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

    /// <summary>
    /// 将字节数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="span">要反序列化的字节数据。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize(Span<byte> span, Type type)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize(stream, type);
    }

    /// <summary>
    /// 将字节数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="memory">要反序列化的字节数据。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize(Memory<byte> memory, Type type)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(memory.Span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize(stream, type);
    }

    /// <summary>
    /// 将字节数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="span">要反序列化的字节数据。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize<T>(Span<byte> span)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Write(span);
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize<T>(stream);
    }

    /// <summary>
    /// 将字节数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="memory">要反序列化的字节数据。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize<T>(Memory<byte> memory)
    {
        using var stream = MemoryStreamHelper.GetRecyclableMemoryStream();
        stream.Seek(0, SeekOrigin.Begin);
        return BsonSerializer.Deserialize<T>(stream);
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <returns>反序列化后的对象。</returns>
    public T Deserialize<T>(byte[] bytes)
    {
        return BsonSerializer.Deserialize<T>(bytes);
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize(byte[] bytes, Type type)
    {
        return BsonSerializer.Deserialize(bytes, type);
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <param name="type">要反序列化的目标类型的类型名字符串。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize(byte[] bytes, string type)
    {
        return BsonSerializer.Deserialize(bytes, Type.GetType(type));
    }

    /// <summary>
    /// 将输入流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="stream">输入流。</param>
    /// <returns>反序列化后的对象。</returns>
    public T Deserialize<T>(Stream stream)
    {
        return BsonSerializer.Deserialize<T>(stream);
    }

    /// <summary>
    /// 将输入流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public object Deserialize(Stream stream, Type type)
    {
        return BsonSerializer.Deserialize(stream, type);
    }

    /// <summary>
    /// 将内存流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <param name="stream">内存流。</param>
    /// <returns>反序列化后的对象。</returns>
    public object DeserializeFrom(Type type, MemoryStream stream)
    {
        return Deserialize(stream, type);
    }

    /// <summary>
    /// 将内存流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="stream">内存流。</param>
    /// <returns>反序列化后的对象。</returns>
    public T DeserializeFrom<T>(MemoryStream stream)
    {
        return Deserialize<T>(stream);
    }

    /// <summary>
    /// 将字节数组中指定范围的数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="bytes">字节数组。</param>
    /// <param name="index">开始索引。</param>
    /// <param name="count">数据长度。</param>
    /// <returns>反序列化后的对象。</returns>
    public T DeserializeFrom<T>(byte[] bytes, int index, int count)
    {
        return BsonSerializer.Deserialize<T>(bytes.AsMemory(index, count).ToArray());
    }

    /// <summary>
    /// 将对象序列化为字节数组。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="t">要序列化的对象。</param>
    /// <returns>序列化后的字节数组。</returns>
    public byte[] SerializeTo<T>(T t)
    {
        return t.ToBson();
    }

    /// <summary>
    /// 将对象序列化并写入到内存中。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="t">要序列化的对象。</param>
    /// <param name="memory">要写入的内存。</param>
    public void SerializeTo<T>(T t, Memory<byte> memory)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
        {
            BsonSerializer.Serialize(writer, typeof(T), t);
        }

        memoryStream.GetBuffer().AsMemory().CopyTo(memory);
    }

    /// <summary>
    /// 将对象序列化并写入到指定的内存流中。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="t">要序列化的对象。</param>
    /// <param name="stream">要写入的内存流。</param>
    public void SerializeTo<T>(T t, MemoryStream stream)
    {
        using var writer = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults);
        BsonSerializer.Serialize(writer, typeof(T), t);
    }

    /// <summary>
    /// 克隆一个对象，通过序列化和反序列化的方式实现。
    /// </summary>
    /// <typeparam name="T">要克隆的对象类型。</typeparam>
    /// <param name="t">要克隆的对象。</param>
    /// <returns>克隆后的对象。</returns>
    public T Clone<T>(T t)
    {
        return Deserialize<T>(t.ToBson());
    }
}
#endif
#if FANTASY_UNITY
using System;
using System.IO;
namespace Fantasy
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
