using System;
using System.Buffers;
using System.IO;
using Fantasy.Assembly;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Fantasy.Serialize
{
    /// <summary>
    /// 代表是一个ProtoBuf协议
    /// </summary>
    public interface IProto { }
    
    /// <summary>
    /// ProtoBufP帮助类
    /// </summary>
    public static class ProtoBufPackHelper
    {
#if FANTASY_NET
        /// <summary>
        /// 初始化方法
        /// </summary>
        public static void Initialize()
        {
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AllowParseableTypes = true;
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AutoCompile = true;
            RuntimeTypeModel.Default.UseImplicitZeroDefaults = true;
            RuntimeTypeModel.Default.InferTagFromNameDefault = true;
            
            foreach (var type in AssemblySystem.ForEach(typeof(IProto)))
            {
                RuntimeTypeModel.Default.Add(type, true);
            }

            RuntimeTypeModel.Default.CompileInPlace();
        }
#endif
#if FANTASY_UNITY
        public static unsafe T Deserialize<T>(byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                using var stream = new UnmanagedMemoryStream(ptr, bytes.Length);
                var @object = Serializer.Deserialize<T>(stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            var @object = RuntimeTypeModel.Default.Deserialize<T>(memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
#endif
#if FANTASY_UNITY
        public static T Deserialize<T>(MemoryStream buffer)
        {
            var @object = Serializer.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(MemoryStream buffer)
        {
            var @object = RuntimeTypeModel.Default.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_UNITY
        public static unsafe object Deserialize(Type type, byte[] bytes)
        {
            fixed (byte* ptr = bytes)
            {
                using var stream = new UnmanagedMemoryStream(ptr, bytes.Length);
                var @object = Serializer.Deserialize(type, stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }

                return @object;
            }
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            var @object = RuntimeTypeModel.Default.Deserialize(type, memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_UNITY
        public static object Deserialize(Type type, MemoryStream buffer)
        {
            var @object = Serializer.Deserialize(type, buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, MemoryStream buffer)
        {
            var @object = RuntimeTypeModel.Default.Deserialize(type, buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_UNITY
        public static unsafe T Deserialize<T>(byte[] bytes, int index, int count)
        {
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                var @object = Serializer.Deserialize<T>(stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
#endif
#if FANTASY_NET
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
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            var @object = RuntimeTypeModel.Default.Deserialize<T>(memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_UNITY
        public static unsafe object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            fixed (byte* ptr = &bytes[index])
            {
                using var stream = new UnmanagedMemoryStream(ptr, count);
                var @object = Serializer.Deserialize(type, stream);
                if (@object is ASerialize aSerialize)
                {
                    aSerialize.AfterDeserialization();
                }
                return @object;
            }
        }
#endif
#if FANTASY_NET
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
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            var @object = RuntimeTypeModel.Default.Deserialize(type, memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
#endif
#if FANTASY_UNITY
        public static void Serialize<T>(T @object, MemoryStream buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }
#endif
#if FANTASY_NET
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        public static void Serialize<T>(T @object, MemoryStream buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            RuntimeTypeModel.Default.Serialize<T>(buffer, @object);
        }
#endif
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public static void Serialize(object @object, MemoryStream buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public static void Serialize(Type type, object @object, MemoryStream buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }

        internal static byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            var buffer = new MemoryStream();
            RuntimeTypeModel.Default.Serialize(buffer, @object);
            return buffer.ToArray();
        }
#if FANTASY_UNITY
        private static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            var buffer = new MemoryStream();
            RuntimeTypeModel.Default.Serialize(buffer, @object);
            return buffer.ToArray();
        }
#endif
#if FANTASY_NET
        private static byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }
            
            var buffer = new MemoryStream();
            RuntimeTypeModel.Default.Serialize<T>(buffer, @object);
            return buffer.ToArray();
        }
#endif
#if FANTASY_NET || FANTASY_UNITY
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
#endif
    }
}