// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable SwapViaDeconstruction
// ReSharper disable InconsistentNaming
namespace Fantasy
{
    public struct PriorityQueueItemUint<T>
    {
        public T Element { get; set; }
        public uint Priority { get; set; }

        public PriorityQueueItemUint(T element, uint priority)
        {
            Element = element;
            Priority = priority;
        }
    }
    
    public struct PriorityQueueItem<T, T1>
    {
        public T Element { get; }
        public T1 Priority { get; }

        public PriorityQueueItem(T element, T1 priority)
        {
            Element = element;
            Priority = priority;
        }
    }
}