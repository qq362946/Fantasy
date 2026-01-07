using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using LightProto.Parser;

namespace LightProto
{
    public static partial class Serializer
    {
#if NET7_0_OR_GREATER
    const string AOTWarning =
        "This method is not fully compatible with AOT compilation. If T is a [ProtoContract] marked type, it should be fine. But for other types, it's not aot safe. Consider using the overload with a IProtoReader<T>/IProtoWriter<T> parameter for aot safe.";
#endif
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied writer.
        /// </summary>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination to write to.</param>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static void SerializeDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(IBufferWriter<byte> destination, T instance) =>
            Serialize(destination, instance, GetProtoWriter<T>());

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination to write to.</param>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static void SerializeDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(Stream destination, T instance) => Serialize(destination, instance, GetProtoWriter<T>());

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static T DeserializeDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(Stream source) => Deserialize(source, GetProtoReader<T>());

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static T DeserializeDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(ReadOnlySequence<byte> source) => Deserialize(source, GetProtoReader<T>());

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static T DeserializeDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(ReadOnlySpan<byte> source) => Deserialize(source, GetProtoReader<T>());

        /// <summary>
        /// Creates a deep clone of the given message.
        /// </summary>
        /// <param name="message">The instance to deep-clone.</param>
        /// <typeparam name="T">The type of the message being cloned.</typeparam>
        /// <returns>A new instance that is a deep clone of <paramref name="message"/>.</returns>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        public static T DeepCloneDynamically<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>(T message) => DeepClone(message, GetProtoReader<T>(), GetProtoWriter<T>());

#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        static IProtoReader<T> GetProtoReader<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>() => (IProtoReader<T>)GetProtoParser(typeof(T), isReader: true);

#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        static IProtoWriter<T> GetProtoWriter<
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            T>()
        {
            return (IProtoWriter<T>)GetProtoParser(typeof(T), isReader: false);
        }

        static readonly ConcurrentDictionary<Type, object> Readers = new();
        static readonly ConcurrentDictionary<Type, object> Writers = new();
        static readonly ConcurrentDictionary<Type, Type> GenericReaderTypes = new();
        static readonly ConcurrentDictionary<Type, Type> GenericWriterTypes = new();

        /// <summary>
        /// Registers a custom ProtoReader and ProtoWriter for type T
        /// </summary>
        static void RegisterParser<T>(IProtoReader<T> reader, IProtoWriter<T> writer)
        {
            Readers.TryAdd(typeof(T), reader);
            Writers.TryAdd(typeof(T), writer);
        }

        static void RegisterGenericParser(Type type, Type readerType, Type writerType)
        {
            GenericReaderTypes.TryAdd(type, readerType);
            GenericWriterTypes.TryAdd(type, writerType);
        }

        static Serializer()
        {
            RegisterParser(BigIntegerProtoParser.ProtoReader, BigIntegerProtoParser.ProtoWriter);
            RegisterParser(BitArrayProtoParser.ProtoReader, BitArrayProtoParser.ProtoWriter);
            RegisterParser(BooleanProtoParser.ProtoReader, BooleanProtoParser.ProtoWriter);
            RegisterParser(ByteArrayProtoParser.ProtoReader, ByteArrayProtoParser.ProtoWriter);
            RegisterParser(ByteListProtoParser.ProtoReader, ByteListProtoParser.ProtoWriter);
            RegisterParser(ComplexProtoParser.ProtoReader, ComplexProtoParser.ProtoWriter);
            RegisterParser(CultureInfoProtoParser.ProtoReader, CultureInfoProtoParser.ProtoWriter);
            RegisterParser(DateTimeProtoParser.ProtoReader, DateTimeProtoParser.ProtoWriter);
            RegisterParser(
                DateTimeOffsetProtoParser.ProtoReader,
                DateTimeOffsetProtoParser.ProtoWriter
            );
            RegisterParser(DecimalProtoParser.ProtoReader, DecimalProtoParser.ProtoWriter);
            RegisterParser(GuidProtoParser.ProtoReader, GuidProtoParser.ProtoWriter);
            RegisterParser(TimeSpanProtoParser.ProtoReader, TimeSpanProtoParser.ProtoWriter);
            RegisterParser(UriProtoParser.ProtoReader, UriProtoParser.ProtoWriter);
            RegisterParser(DoubleProtoParser.ProtoReader, DoubleProtoParser.ProtoWriter);
            RegisterParser(SingleProtoParser.ProtoReader, SingleProtoParser.ProtoWriter);

            RegisterParser(Int32ProtoParser.ProtoReader, Int32ProtoParser.ProtoWriter);
            RegisterParser(UInt32ProtoParser.ProtoReader, UInt32ProtoParser.ProtoWriter);
            RegisterParser(Int64ProtoParser.ProtoReader, Int64ProtoParser.ProtoWriter);
            RegisterParser(UInt64ProtoParser.ProtoReader, UInt64ProtoParser.ProtoWriter);
            RegisterParser(StringProtoParser.ProtoReader, StringProtoParser.ProtoWriter);
            RegisterParser(StringBuilderProtoParser.ProtoReader, StringBuilderProtoParser.ProtoWriter);
            RegisterParser(TimeZoneInfoProtoParser.ProtoReader, TimeZoneInfoProtoParser.ProtoWriter);
            RegisterParser(VersionProtoParser.ProtoReader, VersionProtoParser.ProtoWriter);
#if NET6_0_OR_GREATER
        RegisterParser(DateOnlyProtoParser.ProtoReader, DateOnlyProtoParser.ProtoWriter);
        RegisterParser(TimeOnlyProtoParser.ProtoReader, TimeOnlyProtoParser.ProtoWriter);
        RegisterParser(Int128ProtoParser.ProtoReader, Int128ProtoParser.ProtoWriter);
        RegisterParser(UInt128ProtoParser.ProtoReader, UInt128ProtoParser.ProtoWriter);
        RegisterParser(HalfProtoParser.ProtoReader, HalfProtoParser.ProtoWriter);
        RegisterParser(Matrix3x2ProtoParser.ProtoReader, Matrix3x2ProtoParser.ProtoWriter);
        RegisterParser(Matrix4x4ProtoParser.ProtoReader, Matrix4x4ProtoParser.ProtoWriter);
        RegisterParser(PlaneProtoParser.ProtoReader, PlaneProtoParser.ProtoWriter);
        RegisterParser(QuaternionProtoParser.ProtoReader, QuaternionProtoParser.ProtoWriter);
        RegisterParser(RuneProtoParser.ProtoReader, RuneProtoParser.ProtoWriter);
        RegisterParser(Vector2ProtoParser.ProtoReader, Vector2ProtoParser.ProtoWriter);
        RegisterParser(Vector3ProtoParser.ProtoReader, Vector3ProtoParser.ProtoWriter);
#endif
            RegisterGenericParser(typeof(List<>), typeof(ListProtoReader<>), typeof(ListProtoWriter<>));
            RegisterGenericParser(
                typeof(Nullable<>),
                typeof(NullableProtoReader<>),
                typeof(NullableProtoWriter<>)
            );
            RegisterGenericParser(typeof(Lazy<>), typeof(LazyProtoReader<>), typeof(LazyProtoWriter<>));
            RegisterGenericParser(
                typeof(Stack<>),
                typeof(StackProtoReader<>),
                typeof(StackProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(Queue<>),
                typeof(QueueProtoReader<>),
                typeof(QueueProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(Collection<>),
                typeof(CollectionProtoReader<>),
                typeof(CollectionProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(BlockingCollection<>),
                typeof(BlockingCollectionProtoReader<>),
                typeof(BlockingCollectionProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ConcurrentBag<>),
                typeof(ConcurrentBagProtoReader<>),
                typeof(ConcurrentBagProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ConcurrentQueue<>),
                typeof(ConcurrentQueueProtoReader<>),
                typeof(ConcurrentQueueProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ConcurrentStack<>),
                typeof(ConcurrentStackProtoReader<>),
                typeof(ConcurrentStackProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(HashSet<>),
                typeof(HashSetProtoReader<>),
                typeof(HashSetProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ImmutableArray<>),
                typeof(ImmutableArrayProtoReader<>),
                typeof(ImmutableArrayProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ImmutableList<>),
                typeof(ImmutableListProtoReader<>),
                typeof(ImmutableListProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ImmutableHashSet<>),
                typeof(ImmutableHashSetProtoReader<>),
                typeof(ImmutableHashSetProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(LinkedList<>),
                typeof(LinkedListProtoReader<>),
                typeof(LinkedListProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ObservableCollection<>),
                typeof(ObservableCollectionProtoReader<>),
                typeof(ObservableCollectionProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ReadOnlyCollection<>),
                typeof(ReadOnlyCollectionProtoReader<>),
                typeof(ReadOnlyCollectionProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(ReadOnlyObservableCollection<>),
                typeof(ReadOnlyObservableCollectionProtoReader<>),
                typeof(ReadOnlyObservableCollectionProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(SortedSet<>),
                typeof(SortedSetProtoReader<>),
                typeof(SortedSetProtoWriter<>)
            );
            RegisterGenericParser(
                typeof(KeyValuePair<,>),
                typeof(KeyValuePairProtoReader<,>),
                typeof(KeyValuePairProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(Dictionary<,>),
                typeof(DictionaryProtoReader<,>),
                typeof(DictionaryProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(ReadOnlyDictionary<,>),
                typeof(ReadOnlyDictionaryProtoReader<,>),
                typeof(ReadOnlyDictionaryProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(ConcurrentDictionary<,>),
                typeof(ConcurrentDictionaryProtoReader<,>),
                typeof(ConcurrentDictionaryProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(SortedDictionary<,>),
                typeof(SortedDictionaryProtoReader<,>),
                typeof(SortedDictionaryProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(SortedList<,>),
                typeof(SortedListProtoReader<,>),
                typeof(SortedListProtoWriter<,>)
            );
            RegisterGenericParser(
                typeof(ImmutableDictionary<,>),
                typeof(ImmutableDictionaryProtoReader<,>),
                typeof(ImmutableDictionaryProtoWriter<,>)
            );
        }

#if NET7_0_OR_GREATER
    private const DynamicallyAccessedMemberTypes LightProtoRequiredMembers =
        DynamicallyAccessedMemberTypes.PublicProperties;
#endif

#if NET7_0_OR_GREATER
    [RequiresDynamicCode(AOTWarning)]
#endif
        static object GetProtoParser(
#if NET7_0_OR_GREATER
        [DynamicallyAccessedMembers(LightProtoRequiredMembers)]
#endif
            Type type, bool isReader)
        {
            var parsers = isReader ? Readers : Writers;
            if (parsers.TryGetValue(type, out var obj))
            {
                return obj;
            }

            if (typeof(IProtoParser<>).MakeGenericType(type).IsAssignableFrom(type))
            {
                var parser = type.GetProperty(
                        isReader ? "ProtoReader" : "ProtoWriter",
                        BindingFlags.Public | BindingFlags.Static
                    )!
                    .GetValue(null)!;
                parsers.TryAdd(type, parser);
                return parser;
            }

            if (type.IsEnum)
            {
                var enumParserType = typeof(EnumProtoParser<>).MakeGenericType(type);
                var parser = enumParserType
                    .GetProperty(
                        isReader ? "ProtoReader" : "ProtoWriter",
                        BindingFlags.Public | BindingFlags.Static
                    )!
                    .GetValue(null)!;
                parsers.TryAdd(type, parser);
                return parser;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
#pragma warning disable IL2072
                var elementParser = GetProtoParser(elementType, isReader);
#pragma warning restore IL2072

                var parserType = isReader ? typeof(ArrayProtoReader<>) : typeof(ArrayProtoWriter<>);
                var arrayParserType = parserType.MakeGenericType(elementType);
                uint tag = 0;
                if (isReader == false)
                {
                    // For writers, we need to provide a tag to indicate the field wire type
                    var wireType = (WireFormat.WireType)
#pragma warning disable IL2075
                        elementParser.GetType().GetProperty("WireType")!.GetValue(elementParser)!;
#pragma warning restore IL2075
                    tag = WireFormat.MakeTag(1, wireType);
                }

                var arrayParser = Activator.CreateInstance(arrayParserType, elementParser, tag, 0)!;
                parsers.TryAdd(type, arrayParser);
                return arrayParser;
            }

            if (type.IsGenericType == false)
            {
                throw new InvalidOperationException($"No ProtoParser registered for type {type}");
            }

            var genericDef = type.GetGenericTypeDefinition();
            var genericArguments = type.GetGenericArguments();

            var genericTypes = isReader ? GenericReaderTypes : GenericWriterTypes;

            if (genericArguments.Length == 1)
            {
                var itemType = genericArguments[0];
#pragma warning disable IL2062
                var itemParser = GetProtoParser(itemType, isReader);
#pragma warning restore IL2062
                if (genericTypes.TryGetValue(genericDef, out var genericParserType) == false)
                {
                    if (
                        isReader
                        && typeof(IEnumerable<>).MakeGenericType(itemType).IsAssignableFrom(type)
                    )
                        genericParserType = typeof(ListProtoReader<>);
                    else if (
                        isReader == false
                        && typeof(ICollection<>).MakeGenericType(itemType).IsAssignableFrom(type)
                    )
                        genericParserType = typeof(ICollectionProtoWriter<>);
                    else
                        throw new InvalidOperationException(
                            $"No ProtoParser registered for type {genericDef}"
                        );
                }

                var parserType = genericParserType.MakeGenericType(itemType);
                uint tag = 0;
                if (isReader == false)
                {
                    // For writers, we need to provide a tag to indicate the field wire type
                    var wireType = (WireFormat.WireType)
#pragma warning disable IL2075
                        itemParser.GetType().GetProperty("WireType")!.GetValue(itemParser)!;
#pragma warning restore IL2075
                    tag = WireFormat.MakeTag(1, wireType);
                }

                object parser;
                if (
                    genericParserType == typeof(NullableProtoWriter<>)
                    || genericParserType == typeof(NullableProtoReader<>)
                    || genericParserType == typeof(LazyProtoReader<>)
                    || genericParserType == typeof(LazyProtoWriter<>)
                )
                {
                    parser = Activator.CreateInstance(parserType, itemParser)!;
                }
                else
                {
                    parser = Activator.CreateInstance(parserType, itemParser, tag, 0)!;
                }

                parsers.TryAdd(type, parser);
                return parser;
            }

            if (genericArguments.Length == 2)
            {
                var keyType = genericArguments[0];
                var valueType = genericArguments[1];
                if (genericTypes.TryGetValue(genericDef, out var genericParserType) == false)
                {
                    if (
                        isReader
                        && typeof(IEnumerable<>)
                            .MakeGenericType(
                                typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType)
                            )
                            .IsAssignableFrom(type)
                    )
                        genericParserType = typeof(DictionaryProtoReader<,>);
                    else if (
                        isReader == false
                        && typeof(IReadOnlyDictionary<,>)
                            .MakeGenericType(keyType, valueType)
                            .IsAssignableFrom(type)
                    )
                        genericParserType = typeof(IReadOnlyDictionaryProtoWriter<,>);
                    else
                        throw new InvalidOperationException(
                            $"No ProtoParser registered for type {genericDef}"
                        );
                }

#pragma warning disable IL2062
                var keyParser = GetProtoParser(keyType, isReader);
                var valueParser = GetProtoParser(valueType, isReader);
#pragma warning restore IL2062
                var parserType = genericParserType.MakeGenericType(keyType, valueType);

                object parser;
                if (genericDef == typeof(KeyValuePair<,>))
                {
                    parser = Activator.CreateInstance(parserType, keyParser, valueParser)!;
                }
                else
                {
                    uint tag = WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited);
                    parser = Activator.CreateInstance(parserType, keyParser, valueParser, tag)!;
                }

                parsers.TryAdd(type, parser);
                return parser;
            }

            throw new InvalidOperationException($"No ProtoParser registered for type {type}");
        }
    }
}
