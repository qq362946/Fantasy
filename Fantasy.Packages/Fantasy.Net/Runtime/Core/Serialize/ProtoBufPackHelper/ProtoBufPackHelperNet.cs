#if FANTASY_NET || FANTASY_EXPORTER
using System;
using System.Buffers;
using System.IO;
using System.Linq;
#if !FANTASY_EXPORTER
using Fantasy.Assembly;
using Fantasy.Async;
#endif
using ProtoBuf.Meta;
namespace Fantasy.Serialize
{
    /// <summary>
    /// ProtoBufP帮助类，Net平台使用
    /// </summary>
#if FANTASY_EXPORTER
    public sealed class ProtoBufPackHelper : ISerialize
#endif
#if FANTASY_NET
    public sealed class ProtoBufPackHelper : ISerialize, IAssemblyLifecycle
#endif
    {
        /// <summary>
        /// 序列化器的名字
        /// </summary>
        public string SerializeName { get; } = "ProtoBuf";

        #region AssemblyManifest
#if !FANTASY_EXPORTER
        internal async FTask<ProtoBufPackHelper> Initialize()
        {
#if FANTASY_NET
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AllowParseableTypes = true;
            RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            RuntimeTypeModel.Default.AutoCompile = true;
            RuntimeTypeModel.Default.UseImplicitZeroDefaults = true;
            RuntimeTypeModel.Default.InferTagFromNameDefault = true;
#endif
            await AssemblyLifecycle.Add(this);
            return this;
        }
#endif
#if !FANTASY_EXPORTER
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            var protoBufTypes = assemblyManifest.NetworkProtocolRegistrar.GetNetworkProtocolTypes();
            if (protoBufTypes.Any())
            {
                foreach (var protoBufType in protoBufTypes)
                {
                    RuntimeTypeModel.Default.Add(protoBufType, true);
                }
                RuntimeTypeModel.Default.CompileInPlace();
            }
            await FTask.CompletedTask;
        }
         /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }
#endif
        #endregion

        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="bytes"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            var @object = RuntimeTypeModel.Default.Deserialize<T>(memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="buffer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            var @object = RuntimeTypeModel.Default.Deserialize<T>(buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public object Deserialize(Type type, byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            var @object = RuntimeTypeModel.Default.Deserialize(type, memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }
            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            var @object = RuntimeTypeModel.Default.Deserialize(type, buffer);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            var @object = RuntimeTypeModel.Default.Deserialize<T>(memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf反序列化数据到实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            var @object = RuntimeTypeModel.Default.Deserialize(type, memory);
            if (@object is ASerialize aSerialize)
            {
                aSerialize.AfterDeserialization();
            }

            return @object;
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
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

            RuntimeTypeModel.Default.Serialize<T>(buffer, @object);
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
        /// </summary>
        /// <param name="object"></param>
        /// <param name="buffer"></param>
        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到IBufferWriter中
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

            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到byte[]
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public byte[] Serialize(object @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            using (var buffer = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize(buffer, @object);
                return buffer.ToArray();
            }
        }
        /// <summary>
        /// 使用ProtoBuf序列化某一个实例到byte[]
        /// </summary>
        /// <param name="object"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public byte[] Serialize<T>(T @object)
        {
            if (@object is ASerialize aSerialize)
            {
                aSerialize.BeginInit();
            }

            using (var buffer = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize<T>(buffer, @object);
                return buffer.ToArray();
            }
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
