#if FANTASY_NET
using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy;

public static class MongoHelper
{
    private static readonly HashSet<long> LookupClassMap = new HashSet<long>();
    
    static MongoHelper()
    {
        // 自动注册IgnoreExtraElements
        var conventionPack = new ConventionPack {new IgnoreExtraElementsConvention(true)};
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);
        // BsonSerializer.TryRegisterSerializer(typeof(float2), new StructBsonSerialize<float2>());
        // BsonSerializer.TryRegisterSerializer(typeof(float3), new StructBsonSerialize<float3>());
        // BsonSerializer.TryRegisterSerializer(typeof(float4), new StructBsonSerialize<float4>());
        // BsonSerializer.TryRegisterSerializer(typeof(quaternion), new StructBsonSerialize<quaternion>());
        BsonSerializer.RegisterSerializer(new ObjectSerializer(x => true));
    }

    public static void Initialize()
    {
        // BsonClassMap.LookupClassMap方法用于检索给定类的映射信息，
        // 但MongoDB的C#驱动程序并没有提供直接的方法来卸载已注册的类映射。
        // 一旦一个类的映射被注册到BsonClassMap中，它就会持续存在，因为这些映射被设计为在应用程序的生命周期内保持不变。
        // 为了避免重复注册，这里使用HashSet<long>来记录已注册的程序集。
        // 但这也有一个问题、就是有可能热更程序集的时候添加了新的class，但是HashSet<long>中没有记录，导致无法注册。
        // 从而导致这个class无法正常序列化和反序列化。

        foreach (var type in AssemblySystem.ForEach())
        {
            if (type.IsInterface || type.IsAbstract || type.IsGenericType || !typeof(Entity).IsAssignableFrom(type))
            {
                continue;
            }
                
            BsonClassMap.LookupClassMap(type);
        }
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <returns>反序列化后的对象。</returns>
    public static T Deserialize<T>(byte[] bytes)
    {
        return BsonSerializer.Deserialize<T>(bytes);
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public static object Deserialize(byte[] bytes, Type type)
    {
        return BsonSerializer.Deserialize(bytes, type);
    }

    /// <summary>
    /// 将字节数组反序列化为指定类型的对象。
    /// </summary>
    /// <param name="bytes">要反序列化的字节数组。</param>
    /// <param name="type">要反序列化的目标类型的类型名字符串。</param>
    /// <returns>反序列化后的对象。</returns>
    public static object Deserialize(byte[] bytes, string type)
    {
        return BsonSerializer.Deserialize(bytes, Type.GetType(type));
    }

    /// <summary>
    /// 将输入流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="stream">输入流。</param>
    /// <returns>反序列化后的对象。</returns>
    public static T Deserialize<T>(Stream stream) where T : new()
    {
        return BsonSerializer.Deserialize<T>(stream);
    }

    /// <summary>
    /// 将输入流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <returns>反序列化后的对象。</returns>
    public static object Deserialize(Stream stream, Type type)
    {
        return BsonSerializer.Deserialize(stream, type);
    }

    /// <summary>
    /// 将内存流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <param name="type">要反序列化的目标类型。</param>
    /// <param name="stream">内存流。</param>
    /// <returns>反序列化后的对象。</returns>
    public static object DeserializeFrom(Type type, MemoryStream stream)
    {
        return BsonSerializer.Deserialize(stream, type);
    }

    /// <summary>
    /// 将内存流中的数据反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    /// <param name="stream">内存流。</param>
    /// <returns>反序列化后的对象。</returns>
    public static T DeserializeFrom<T>(MemoryStream stream) where T : new()
    {
        return Deserialize<T>(stream);
    }

    // /// <summary>
    // /// 将字节数组中指定范围的数据反序列化为指定类型的对象。
    // /// </summary>
    // /// <typeparam name="T">要反序列化的目标类型。</typeparam>
    // /// <param name="bytes">字节数组。</param>
    // /// <param name="index">开始索引。</param>
    // /// <param name="count">数据长度。</param>
    // /// <returns>反序列化后的对象。</returns>
    // public T DeserializeFrom<T>(byte[] bytes, int index, int count) where T : new()
    // {
    //     var asMemory = bytes.AsMemory(index, count);
    //     
    //     
    //     return BsonSerializer.Deserialize<T>(asMemory.ToArray());
    // }

    /// <summary>
    /// 将对象序列化为字节数组。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="t">要序列化的对象。</param>
    /// <returns>序列化后的字节数组。</returns>
    public static byte[] SerializeTo<T>(T t)
    {
        if (t is ISupportInitialize supportInitialize)
        {
            supportInitialize.BeginInit();
        }
        
        return t.ToBson();
    }

    /// <summary>
    /// 将对象序列化并写入到指定的内存流中。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="t">要序列化的对象。</param>
    /// <param name="stream">要写入的内存流。</param>
    public static void SerializeTo<T>(T t, MemoryStream stream)
    {
        if (t is ISupportInitialize supportInitialize)
        {
            supportInitialize.BeginInit();
        }
        
        using var writer = new BsonBinaryWriter(stream, BsonBinaryWriterSettings.Defaults);
        var context = BsonSerializationContext.CreateRoot(writer);
        BsonSerializationArgs args = default;
        args.NominalType = typeof (object);
        var serializer = BsonSerializer.LookupSerializer(args.NominalType);
        serializer.Serialize(context, args, t);
    }

    /// <summary>
    /// 克隆一个对象，通过序列化和反序列化的方式实现。
    /// </summary>
    /// <typeparam name="T">要克隆的对象类型。</typeparam>
    /// <param name="t">要克隆的对象。</param>
    /// <returns>克隆后的对象。</returns>
    public static T Clone<T>(T t)
    {
        return Deserialize<T>(SerializeTo(t));
    }
}
#endif
