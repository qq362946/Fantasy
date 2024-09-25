using System;
using System.Collections.Generic;
using Fantasy.Assembly;
using Fantasy.Helper;
using Fantasy.Network;
using ProtoBuf;
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy.Serialize
{
    /// <summary>
    /// 框架内置的序列化器类型
    /// </summary>
    public static class FantasySerializerType
    {
        /// <summary>
        /// ProtoBuf在SerializerManager的数组下标
        /// </summary>
        public const int ProtoBuf = 0;
        /// <summary>
        /// Bson在SerializerManager的数组下标
        /// </summary>
        public const int Bson = 1;

        /// <summary>
        /// 用户自定义的序列化器下标映射
        /// </summary>
        public const int Protocal = 2;
    }
    
    /// <summary>
    /// 管理序列化静态方法，主要是优化网络协议时使用。
    /// </summary>
    public static class SerializerManager
    {
        private static ISerialize[] _serializers;
        private static bool _isInitialized = false;

#if FANTASY_NET || FANTASY_UNITY

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="customeSerialize">自定义序列化器 默认使用ProtoBuf</param>
        public static void Initialize(ISerialize customeSerialize = null)
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                _serializers = new ISerialize[3];
#if FANTASY_NET
                var bsonSerializeHelper = (ISerialize)Activator.CreateInstance<BsonPackHelper>();
                _serializers[FantasySerializerType.Bson] = bsonSerializeHelper;
#endif

                var protobufSerializeHelper = (ISerialize)Activator.CreateInstance<ProtoBufPackHelper>();
                _serializers[FantasySerializerType.ProtoBuf] = protobufSerializeHelper;

                //没有传入序列化器 默认框架内所有的协议都在内置Pb
                _serializers[FantasySerializerType.Protocal] = customeSerialize != null ? customeSerialize : protobufSerializeHelper;


                _isInitialized = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }
#else
        /// <summary>
        /// 初始化方法
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            _serializers = new ISerialize[1];
            _serializers[0] = new ProtoBufPackHelper();
        }
#endif

        /// <summary>
        /// 销毁方法
        /// </summary>
        public static void Dispose()
        {
            _isInitialized = false;
            Array.Clear(_serializers, 0, _serializers.Length);
        }

        /// <summary>
        /// 根据协议类型获取序列化器
        /// </summary>
        /// <param name="opCodeProtocolType"></param>
        /// <returns></returns>
        public static ISerialize GetSerializer(uint opCodeProtocolType)
        {
            return _serializers[opCodeProtocolType];
        }
        
        /// <summary>
        /// 获得一个序列化器
        /// </summary>
        /// <param name="opCodeProtocolType"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static bool TryGetSerializer(uint opCodeProtocolType, out ISerialize serializer)
        {
            if (opCodeProtocolType < _serializers.Length)
            {
                serializer = _serializers[opCodeProtocolType];
                return true;
            }

            serializer = default;
            return false;
        }
    }
}