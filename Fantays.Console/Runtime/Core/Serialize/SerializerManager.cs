using System;
using System.Collections.Generic;
using Fantasy.Assembly;
using Fantasy.Helper;
#if !FANTASY_EXPORTER
using Fantasy.Network;
#endif
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
        /// 初始化方法
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                var sort = new SortedList<long, ISerialize>();

                foreach (var serializerType in AssemblySystem.ForEach(typeof(ISerialize)))
                {
                    var serializer = (ISerialize)Activator.CreateInstance(serializerType);
                    var computeHash64 = HashCodeHelper.ComputeHash64(serializer.SerializeName);
                    sort.Add(computeHash64, serializer);
                }

                var index = 1;
                _serializers = new ISerialize[sort.Count];

                foreach (var (_, serialize) in sort)
                {
                    var serializerIndex = 0;

                    switch (serialize)
                    {
                        case ProtoBufPackHelper:
                        {
                            serializerIndex = FantasySerializerType.ProtoBuf;
                            break;
                        }
                        case BsonPackHelper:
                        {
                            serializerIndex = FantasySerializerType.Bson;
                            break;
                        }
                        default:
                        {
                            serializerIndex = ++index;
                            break;
                        }
                    }

                    _serializers[serializerIndex] = serialize;
                }

                _isInitialized = true;
            }
            catch (Exception e)
            {
                Log.Error(e);
                Dispose();
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