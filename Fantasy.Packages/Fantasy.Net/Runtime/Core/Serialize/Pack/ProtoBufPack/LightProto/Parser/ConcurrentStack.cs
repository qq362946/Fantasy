using System.Collections.Concurrent;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace LightProto.Parser
{
    public sealed class ConcurrentStackProtoWriter<T> : IEnumerableProtoWriter<ConcurrentStack<T>, T>
    {
        public ConcurrentStackProtoWriter(IProtoWriter<T> itemWriter, uint tag, int itemFixedSize)
            : base(itemWriter, tag, static collection => collection.Count, itemFixedSize) { }
    }

    public sealed class ConcurrentStackProtoReader<T> : IEnumerableProtoReader<ConcurrentStack<T>, T>
    {
        public ConcurrentStackProtoReader(IProtoReader<T> itemReader, uint tag, int itemFixedSize)
            : base(
                itemReader,
                static _ => new ConcurrentStack<T>(),
                static (collection, item) =>
                {
                    collection.Push(item);
                    return collection;
                },
                itemFixedSize,
                stack => new ConcurrentStack<T>(stack)
            ) { }
    }
}
