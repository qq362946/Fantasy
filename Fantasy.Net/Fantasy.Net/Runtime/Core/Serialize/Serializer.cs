using NativeCollections;

namespace Fantasy.Serialize
{
    /// <summary>
    /// </summary>
    public static unsafe class SerializerManager
    {
        private static Serializer* _serializers;

        /// <summary>
        /// </summary>
        public static void Initialize()
        {
            _serializers = (Serializer*)NativeMemoryAllocator.Alloc(3 * sizeof(Serializer));
            _serializers[0] = new Serializer(&BsonPackHelper.Serialize, &BsonPackHelper.Deserialize);
            _serializers[1] = new Serializer(&MemoryPackHelper.Serialize, &MemoryPackHelper.Deserialize);
            _serializers[2] = new Serializer(&ProtoBufPackHelper.Serialize, &ProtoBufPackHelper.Deserialize);
        }

        /// <summary>
        /// </summary>
        public static void Dispose()
        {
            NativeMemoryAllocator.Free(_serializers);
            _serializers = null;
        }

        /// <summary>
        /// </summary>
        public static bool TryGetSerializer(uint opCodeProtocolType, out Serializer serializer)
        {
            opCodeProtocolType--;
            if (opCodeProtocolType < 3)
            {
                serializer = _serializers[opCodeProtocolType];
                return true;
            }

            serializer = default;
            return false;
        }
    }

    /// <summary>
    /// </summary>
    public readonly unsafe ref struct Serializer
    {
        private readonly delegate* managed<Type, object, MemoryStreamBuffer, void> _serialize;
        private readonly delegate* managed<Type, MemoryStreamBuffer, object> _deserialize;

        /// <summary>
        /// </summary>
        public Serializer(delegate*<Type, object, MemoryStreamBuffer, void> serialize, delegate*<Type, MemoryStreamBuffer, object> deserialize)
        {
            _serialize = serialize;
            _deserialize = deserialize;
        }

        /// <summary>
        /// </summary>
        public void Serialize(Type type, object @object, MemoryStreamBuffer buffer)
        {
            _serialize(type, @object, buffer);
        }

        /// <summary>
        /// </summary>
        public object Deserialize(Type type, MemoryStreamBuffer buffer)
        {
            return _deserialize(type, buffer);
        }
    }
}