using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Fantasy.Assembly;
using Fantasy.Async;
using ProtoBuf.Meta;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace Fantasy.Serialize
{
    /// <summary>
    /// ProtoBuf 序列化实现，基于 protobuf-net 提供高性能的二进制序列化/反序列化功能。
    /// 支持 ASerialize 接口的序列化生命周期回调，适用于网络通信和数据持久化。
    /// </summary>
#if FANTASY_EXPORTER
    public sealed class ProtoBufPackHelper : ISerialize
#endif
#if FANTASY_NET || FANTASY_UNITY
    public sealed class ProtoBufPackHelper : ISerialize, IAssemblyLifecycle
#endif
    {
        /// <inheritdoc/>
        public string SerializeName { get; } = "ProtoBuf";

        [ThreadStatic] private static MemoryStream? _cachedStream;

        #region AssemblyManifest

#if !FANTASY_EXPORTER
        /// <summary>
        /// 初始化 ProtoBuf 序列化器并注册到程序集生命周期管理。
        /// </summary>
        /// <returns>初始化后的 ProtoBufPackHelper 实例</returns>
        internal async FTask<ProtoBufPackHelper> Initialize()
        {
            // 配置 ProtoBuf 运行时模型
            RuntimeTypeModel.Default.AutoAddMissingTypes = true; // 自动添加缺失的类型
            RuntimeTypeModel.Default.AllowParseableTypes = true; // 允许可解析类型
            RuntimeTypeModel.Default.AutoAddMissingTypes = true; // 自动添加缺失的类型
            RuntimeTypeModel.Default.AutoCompile = true; // 自动编译模型以提升性能
            RuntimeTypeModel.Default.UseImplicitZeroDefaults = true; // 使用隐式零默认值
            RuntimeTypeModel.Default.InferTagFromNameDefault = true; // 从名称推断标签

            await AssemblyLifecycle.Add(this);
            return this;
        }
#endif
#if !FANTASY_EXPORTER
        /// <summary>
        /// 程序集加载时的回调，注册所有网络协议类型到 ProtoBuf 运行时模型。
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            var protoBufTypes = assemblyManifest.NetworkProtocolRegistrar.GetNetworkProtocolTypes();
            if (protoBufTypes.Any())
            {
                // 将所有网络协议类型注册到 ProtoBuf 模型
                foreach (var protoBufType in protoBufTypes)
                {
                    RuntimeTypeModel.Default.Add(protoBufType, true);
                }
#if UNITY_EDITOR || !ENABLE_IL2CPP
                // 编译模型以提高序列化/反序列化性能
                // 在 AOT 环境下跳过编译，因为动态代码生成不被支持
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
                    RuntimeTypeModel.Default.CompileInPlace();
                }
#endif
            }

            await FTask.CompletedTask;
        }

        /// <summary>
        /// 程序集卸载时的回调。
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象，包含程序集的元数据和注册器</param>
        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }
#endif

        #endregion

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            return RuntimeTypeModel.Default.Deserialize<T>(memory);
        }

        /// <inheritdoc/>
        public T Deserialize<T>(MemoryStreamBuffer buffer)
        {
            return RuntimeTypeModel.Default.Deserialize<T>(buffer);
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, byte[] bytes)
        {
            var memory = new ReadOnlyMemory<byte>(bytes);
            return RuntimeTypeModel.Default.Deserialize(type, memory);
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            return RuntimeTypeModel.Default.Deserialize(type, buffer);
        }

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes, int index, int count)
        {
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            return RuntimeTypeModel.Default.Deserialize<T>(memory);
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            var memory = new ReadOnlyMemory<byte>(bytes, index, count);
            return RuntimeTypeModel.Default.Deserialize(type, memory);
        }

        /// <inheritdoc/>
        public void Serialize<T>(T @object, IBufferWriter<byte> buffer)
        {
            RuntimeTypeModel.Default.Serialize<T>(buffer, @object);
        }

        /// <inheritdoc/>
        public void Serialize(object @object, IBufferWriter<byte> buffer)
        {
            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }

        /// <inheritdoc/>
        public void Serialize(Type type, object @object, IBufferWriter<byte> buffer)
        {
            RuntimeTypeModel.Default.Serialize(buffer, @object);
        }

        /// <inheritdoc/>
        public byte[] Serialize(object @object)
        {
            if (_cachedStream == null)
            {
                _cachedStream = new MemoryStream();
            }
            else
            {
                _cachedStream.SetLength(0);
                _cachedStream.Position = 0;
            }

            RuntimeTypeModel.Default.Serialize(_cachedStream, @object);
            return _cachedStream.ToArray();
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T @object)
        {
            if (_cachedStream == null)
            {
                _cachedStream = new MemoryStream();
            }
            else
            {
                _cachedStream.SetLength(0);
                _cachedStream.Position = 0;
            }

            RuntimeTypeModel.Default.Serialize(_cachedStream, @object);
            return _cachedStream.ToArray();
        }

        /// <inheritdoc/>
        public T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}
