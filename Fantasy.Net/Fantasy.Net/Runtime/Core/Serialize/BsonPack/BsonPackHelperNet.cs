#if FANTASY_NET
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Fantasy.Assembly;
using Fantasy.Entitas;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Fantasy.Serialize
{
    /// <summary>
    /// BSON帮助方法
    /// </summary>
    public class BsonPackHelper : ISerialize
    {
        /// <summary>
        /// 序列化器的名字
        /// </summary>
        public string SerializeName { get; } = "Bson";
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public BsonPackHelper()
        {
            // 清除掉注册过的LookupClassMap。

            var classMapRegistryField = typeof(BsonClassMap).GetField("__classMaps", BindingFlags.Static | BindingFlags.NonPublic);

            if (classMapRegistryField != null)
            {
                ((Dictionary<Type, BsonClassMap>)classMapRegistryField.GetValue(null)).Clear();
            }

            // 清除掉注册过的ConventionRegistry。

            var registryField = typeof(ConventionRegistry).GetField("_lookup", BindingFlags.Static | BindingFlags.NonPublic);

            if (registryField != null)
            {
                var registry = registryField.GetValue(null);
                var dictionaryField = registry.GetType().GetField("_conventions", BindingFlags.Instance | BindingFlags.NonPublic);
                if (dictionaryField != null)
                {
                    ((IDictionary)dictionaryField.GetValue(registry)).Clear();
                }
            }

            // 初始化ConventionRegistry、注册IgnoreExtraElements。

            ConventionRegistry.Register("IgnoreExtraElements", new ConventionPack { new IgnoreExtraElementsConvention(true) }, type => true);

            // 注册一个自定义的序列化器。

            // BsonSerializer.TryRegisterSerializer(typeof(float2), new StructBsonSerialize<float2>());
            // BsonSerializer.TryRegisterSerializer(typeof(float3), new StructBsonSerialize<float3>());
            // BsonSerializer.TryRegisterSerializer(typeof(float4), new StructBsonSerialize<float4>());
            // BsonSerializer.TryRegisterSerializer(typeof(quaternion), new StructBsonSerialize<quaternion>());
            BsonSerializer.RegisterSerializer(new ObjectSerializer(x => true));

            // 注册LookupClassMap。

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
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            var @object = BsonSerializer.Deserialize<T>(bytes);
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
        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = BsonSerializer.Deserialize<T>(buffer);
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
        public object Deserialize(Type type, byte[] bytes)
        {
            var @object = BsonSerializer.Deserialize(bytes, type);
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
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = BsonSerializer.Deserialize(buffer, type);
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
        public unsafe T Deserialize<T>(byte[] bytes, int index, int count)
        {
            T @object;
            
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                @object = BsonSerializer.Deserialize<T>(stream);
            }

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
        public unsafe object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            object @object;
            
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                @object = BsonSerializer.Deserialize(stream, type);
            }
            
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
        public void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            using IBsonWriter bsonWriter =
                new BsonBinaryWriter((MemoryStream)buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            using IBsonWriter bsonWriter =
                new BsonBinaryWriter((MemoryStream)buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, @object.GetType(), @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            using IBsonWriter bsonWriter =
                new BsonBinaryWriter((MemoryStream)buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, type, @object);
        }

        /// <summary>
        /// 序列化并返回的长度
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int SerializeAndReturnLength(Type type, object @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            using IBsonWriter bsonWriter = new BsonBinaryWriter(buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, type, @object);
            return (int)buffer.Length;
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
            return @object.ToBson(@object.GetType());
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
            return @object.ToBson<T>();
        }
    
        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}
#endif