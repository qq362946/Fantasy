#if FANTASY_NET
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Fantasy
{
    public static class BsonPackHelper
    {
        #region Initialize

        public static void Initialize()
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

        #endregion

        public static T Deserialize<T>(byte[] bytes)
        {
            var @object = BsonSerializer.Deserialize<T>(bytes);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = BsonSerializer.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
    
        public static object Deserialize(Type type, byte[] bytes)
        {
            var @object = BsonSerializer.Deserialize(bytes, type);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
    
        public static object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = BsonSerializer.Deserialize(buffer, type);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }

        public static unsafe T Deserialize<T>(byte[] bytes, int index, int count)
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

        public static unsafe object Deserialize(Type type, byte[] bytes, int index, int count)
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

        public static void Serialize<T>(T @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            using IBsonWriter bsonWriter = new BsonBinaryWriter(buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, @object);
        }

        public static void Serialize(Type type, object @object, MemoryStreamBuffer buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            using IBsonWriter bsonWriter = new BsonBinaryWriter(buffer, BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(bsonWriter, type, @object);
        }

        public static byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            return @object.ToBson(@object.GetType());
        }
    
        public static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            return @object.ToBson<T>();
        }
    
        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}
#endif