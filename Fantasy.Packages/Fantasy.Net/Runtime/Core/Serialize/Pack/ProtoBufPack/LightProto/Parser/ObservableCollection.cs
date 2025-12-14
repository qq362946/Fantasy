using System.Collections.ObjectModel;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace LightProto.Parser
{
    public sealed class ObservableCollectionProtoWriter<T>
        : IEnumerableProtoWriter<ObservableCollection<T>, T>
    {
        public ObservableCollectionProtoWriter(IProtoWriter<T> itemWriter, uint tag, int itemFixedSize)
            : base(itemWriter, tag, static collection => collection.Count, itemFixedSize) { }
    }

    public sealed class ObservableCollectionProtoReader<T>
        : IEnumerableProtoReader<ObservableCollection<T>, T>
    {
        public ObservableCollectionProtoReader(IProtoReader<T> itemReader, uint tag, int itemFixedSize)
            : this(itemReader, itemFixedSize) { }

        public ObservableCollectionProtoReader(IProtoReader<T> itemReader, int itemFixedSize)
            : base(
                itemReader,
                static (capacity) => new ObservableCollection<T>(),
                static (collection, item) =>
                {
                    collection.Add(item);
                    return collection;
                },
                itemFixedSize
            ) { }
    }
}
