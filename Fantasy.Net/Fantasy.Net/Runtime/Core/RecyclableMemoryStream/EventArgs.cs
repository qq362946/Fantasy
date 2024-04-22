namespace Fantasy
{
    using System;

    /// <summary>
    /// 提供用于管理可回收内存流的分部类。
    /// </summary>
    public sealed partial class RecyclableMemoryStreamManager
    {
        /// <summary>
        /// 用于 <see cref="StreamCreated"/> 事件的参数类。
        /// </summary>
        public sealed class StreamCreatedEventArgs : EventArgs
        {
            /// <summary>
            /// Stream流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 请求的流大小。
            /// </summary>
            public long RequestedSize { get; }

            /// <summary>
            /// 实际的流大小。
            /// </summary>
            public long ActualSize { get; }

            /// <summary>
            /// 初始化 <see cref="StreamCreatedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="requestedSize">请求的流大小。</param>
            /// <param name="actualSize">实际的流大小。</param>
            public StreamCreatedEventArgs(Guid guid, string tag, long requestedSize, long actualSize)
            {
                this.Id = guid;
                this.Tag = tag;
                this.RequestedSize = requestedSize;
                this.ActualSize = actualSize;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamDisposed"/> 事件的参数类。
        /// </summary>
        public sealed class StreamDisposedEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 分配流的堆栈
            /// </summary>
            public string AllocationStack { get; }

            /// <summary>
            ///处置流的堆栈。
            /// </summary>
            public string DisposeStack { get; }

            /// <summary>
            /// 流的生命周期。
            /// </summary>
            public TimeSpan Lifetime { get; }

            /// <summary>
            /// 初始化 <see cref="StreamDisposedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="allocationStack">原始分配的堆栈。</param>
            /// <param name="disposeStack">处置堆栈。</param>
            [Obsolete("Use another constructor override")]
            public StreamDisposedEventArgs(Guid guid, string tag, string allocationStack, string disposeStack)
                :this(guid, tag, TimeSpan.Zero, allocationStack, disposeStack)
            {
                
            }

            /// <summary>
            /// 初始化 <see cref="StreamDisposedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="lifetime">流的生命周期。</param>
            /// <param name="allocationStack">原始分配的堆栈。</param>
            /// <param name="disposeStack">处置堆栈。</param>
            public StreamDisposedEventArgs(Guid guid, string tag, TimeSpan lifetime, string allocationStack, string disposeStack)
            {
                this.Id = guid;
                this.Tag = tag;
                this.Lifetime = lifetime;
                this.AllocationStack = allocationStack;
                this.DisposeStack = disposeStack;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamDoubleDisposed"/> 事件的参数类。
        /// </summary>
        public sealed class StreamDoubleDisposedEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 分配流的堆栈。
            /// </summary>
            public string AllocationStack { get; }

            /// <summary>
            /// 第一个处置堆栈。
            /// </summary>
            public string DisposeStack1 { get; }

            /// <summary>
            /// 第二个处置堆栈。
            /// </summary>
            public string DisposeStack2 { get; }

            /// <summary>
            /// 初始化 <see cref="StreamDoubleDisposedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="allocationStack">原始分配的堆栈。</param>
            /// <param name="disposeStack1">第一个处置堆栈。</param>
            /// <param name="disposeStack2">第二个处置堆栈。</param>
            public StreamDoubleDisposedEventArgs(Guid guid, string tag, string allocationStack, string disposeStack1, string disposeStack2)
            {
                this.Id = guid;
                this.Tag = tag;
                this.AllocationStack = allocationStack;
                this.DisposeStack1 = disposeStack1;
                this.DisposeStack2 = disposeStack2;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamFinalized"/> 事件的参数类。
        /// </summary>
        public sealed class StreamFinalizedEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 分配流的堆栈。
            /// </summary>
            public string AllocationStack { get; }

            /// <summary>
            /// 初始化 <see cref="StreamFinalizedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="allocationStack">原始分配的堆栈。</param>
            public StreamFinalizedEventArgs(Guid guid, string tag, string allocationStack)
            {
                this.Id = guid;
                this.Tag = tag;
                this.AllocationStack = allocationStack;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamConvertedToArray"/> 事件的参数类。
        /// </summary>
        public sealed class StreamConvertedToArrayEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 调用 ToArray 的堆栈。
            /// </summary>
            public string Stack { get; }

            /// <summary>
            /// 堆栈的长度。
            /// </summary>
            public long Length { get; }

            /// <summary>
            /// 初始化 <see cref="StreamConvertedToArrayEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="stack">ToArray 调用的堆栈。</param>
            /// <param name="length">流的长度。</param>
            public StreamConvertedToArrayEventArgs(Guid guid, string tag, string stack, long length)
            {
                this.Id = guid;
                this.Tag = tag;
                this.Stack = stack;
                this.Length = length;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamOverCapacity"/> 事件的参数类。
        /// </summary>
        public sealed class StreamOverCapacityEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 原始分配堆栈。
            /// </summary>
            public string AllocationStack { get; }

            /// <summary>
            /// 请求的容量。
            /// </summary>
            public long RequestedCapacity { get; }

            /// <summary>
            /// 最大容量。
            /// </summary>
            public long MaximumCapacity { get; }

            /// <summary>
            /// 初始化 <see cref="StreamOverCapacityEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="requestedCapacity">请求的容量。</param>
            /// <param name="maximumCapacity">管理器的最大流容量。</param>
            /// <param name="allocationStack">原始分配堆栈。</param>
            internal StreamOverCapacityEventArgs(Guid guid, string tag, long requestedCapacity, long maximumCapacity, string allocationStack)
            {
                this.Id = guid;
                this.Tag = tag;
                this.RequestedCapacity = requestedCapacity;
                this.MaximumCapacity = maximumCapacity;
                this.AllocationStack = allocationStack;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="BlockCreated"/> 事件的参数类。
        /// </summary>
        public sealed class BlockCreatedEventArgs : EventArgs
        {
            /// <summary>
            /// 当前从小型池中使用的字节数。
            /// </summary>
            public long SmallPoolInUse { get; }

            /// <summary>
            /// 初始化 <see cref="BlockCreatedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="smallPoolInUse">当前从小型池中使用的字节数。</param>
            internal BlockCreatedEventArgs(long smallPoolInUse)
            {
                this.SmallPoolInUse = smallPoolInUse;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="LargeBufferCreated"/> 事件的参数类。
        /// </summary>
        public sealed class LargeBufferCreatedEventArgs : EventArgs
        {
            /// <summary>
            ///  流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 缓冲区是否满足来自池的需求。
            /// </summary>
            public bool Pooled { get; }

            /// <summary>
            /// 所需的缓冲区大小。
            /// </summary>
            public long RequiredSize { get; }

            /// <summary>
            /// 从大型池中当前使用的字节数。
            /// </summary>
            public long LargePoolInUse { get; }

            /// <summary>
            /// 如果缓冲区未从池中满足需求，并且 <see cref="GenerateCallStacks"/> 已打开，
            /// 则包含分配请求的调用堆栈。
            /// </summary>
            public string CallStack { get; }

            /// <summary>
            /// 初始化 <see cref="LargeBufferCreatedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="requiredSize">新缓冲区的所需大小。</param>
            /// <param name="largePoolInUse">从大型池中当前使用的字节数。</param>
            /// <param name="pooled">缓冲区是否满足来自池的需求。</param>
            /// <param name="callStack">分配请求的调用堆栈（如果未从池中满足需求并启用了 <see cref="GenerateCallStacks"/>）。</param>
            internal LargeBufferCreatedEventArgs(Guid guid, string tag, long requiredSize, long largePoolInUse, bool pooled, string callStack)
            {
                this.RequiredSize = requiredSize;
                this.LargePoolInUse = largePoolInUse;
                this.Pooled = pooled;
                this.Id = guid;
                this.Tag = tag;
                this.CallStack = callStack;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="BufferDiscarded"/> 事件的参数类。
        /// </summary>
        public sealed class BufferDiscardedEventArgs : EventArgs
        {
            /// <summary>
            /// 流的唯一 ID。
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// 可选的事件标签。
            /// </summary>
            public string Tag { get; }

            /// <summary>
            /// 缓冲区的类型。
            /// </summary>
            public Events.MemoryStreamBufferType BufferType { get; }

            /// <summary>
            /// 丢弃此缓冲区的原因。
            /// </summary>
            public Events.MemoryStreamDiscardReason Reason { get; }

            /// <summary>
            /// 初始化 <see cref="BufferDiscardedEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="guid">流的唯一 ID。</param>
            /// <param name="tag">流的标签。</param>
            /// <param name="bufferType">正在丢弃的缓冲区的类型。</param>
            /// <param name="reason">丢弃缓冲区的原因。</param>
            internal BufferDiscardedEventArgs(Guid guid, string tag, Events.MemoryStreamBufferType bufferType, Events.MemoryStreamDiscardReason reason)
            {
                this.Id = guid;
                this.Tag = tag;
                this.BufferType = bufferType;
                this.Reason = reason;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="StreamLength"/> 事件的参数类。
        /// </summary>
        public sealed class StreamLengthEventArgs : EventArgs
        {
            /// <summary>
            /// 流的长度。
            /// </summary>
            public long Length { get; }

            /// <summary>
            /// 初始化 <see cref="StreamLengthEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="length">流的长度。</param>
            public StreamLengthEventArgs(long length)
            {
                this.Length = length;
            }
        }

        /// <summary>
        /// 提供用于 <see cref="UsageReport"/> 事件的参数类。
        /// </summary>
        public sealed class UsageReportEventArgs : EventArgs
        {
            /// <summary>
            /// 当前正在使用的小型池字节数。
            /// </summary>
            public long SmallPoolInUseBytes { get; }

            /// <summary>
            /// 当前可用的小型池字节数。
            /// </summary>
            public long SmallPoolFreeBytes { get; }

            /// <summary>
            /// 当前正在使用的大型池字节数。
            /// </summary>
            public long LargePoolInUseBytes { get; }

            /// <summary>
            /// 当前可用的大型池字节数。
            /// </summary>
            public long LargePoolFreeBytes { get; }

            /// <summary>
            /// 初始化 <see cref="UsageReportEventArgs"/> 类的新实例。
            /// </summary>
            /// <param name="smallPoolInUseBytes">当前正在使用的小型池字节数。</param>
            /// <param name="smallPoolFreeBytes">当前可用的小型池字节数。</param>
            /// <param name="largePoolInUseBytes">当前正在使用的大型池字节数。</param>
            /// <param name="largePoolFreeBytes">当前可用的大型池字节数。</param>
            public UsageReportEventArgs(
                long smallPoolInUseBytes,
                long smallPoolFreeBytes,
                long largePoolInUseBytes,
                long largePoolFreeBytes)
            {
                this.SmallPoolInUseBytes = smallPoolInUseBytes;
                this.SmallPoolFreeBytes = smallPoolFreeBytes;
                this.LargePoolInUseBytes = largePoolInUseBytes;
                this.LargePoolFreeBytes = largePoolFreeBytes;
            }
        }
    }
}
