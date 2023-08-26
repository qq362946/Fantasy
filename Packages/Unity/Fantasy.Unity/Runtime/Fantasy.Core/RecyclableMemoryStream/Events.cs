// ---------------------------------------------------------------------
// Copyright (c) 2015 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------

namespace Fantasy.IO
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// 提供用于管理可回收内存流的功能的管理器类。此管理器负责创建和管理内存流，并提供了对内存分配和回收的细粒度控制，以最大程度地减少内存分配和垃圾回收的开销。
    /// </summary>
    public sealed partial class RecyclableMemoryStreamManager
    {
        /// <summary>
        /// 用于 RecyclableMemoryStream 的 ETW 事件。
        /// </summary>
        [EventSource(Name = "Microsoft-IO-RecyclableMemoryStream", Guid = "{B80CD4E4-890E-468D-9CBA-90EB7C82DFC7}")]
        public sealed class Events : EventSource
        {
            /// <summary>
            /// 静态日志对象，通过它写入所有事件。
            /// </summary>
            public static Events Writer = new();

            /// <summary>
            /// 缓冲区类型枚举。
            /// </summary>
            public enum MemoryStreamBufferType
            {
                /// <summary>
                /// 小块缓冲区。
                /// </summary>
                Small,
                /// <summary>
                /// 大池缓冲区。
                /// </summary>
                Large
            }

            /// <summary>
            /// 丢弃缓冲区的可能原因枚举。
            /// </summary>
            public enum MemoryStreamDiscardReason
            {
                /// <summary>
                /// 缓冲区太大，无法重新放入池中。
                /// </summary>
                TooLarge,
                /// <summary>
                /// 池中有足够的空闲字节。
                /// </summary>
                EnoughFree
            }

            /// <summary>
            /// 在创建流对象时记录的事件。
            /// </summary>
            /// <param name="guid">此流的唯一 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="requestedSize">流的请求大小。</param>
            /// <param name="actualSize">从池中分配给流的实际大小。</param>
            [Event(1, Level = EventLevel.Verbose, Version = 2)]
            public void MemoryStreamCreated(Guid guid, string tag, long requestedSize, long actualSize)
            {
                if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
                {
                    WriteEvent(1, guid, tag ?? string.Empty, requestedSize, actualSize);
                }
            }

            /// <summary>
            /// 当流被释放时记录的事件。
            /// </summary>
            /// <param name="guid">此流的唯一 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="lifetimeMs">流的生命周期（毫秒）。</param>
            /// <param name="allocationStack">初始分配的调用堆栈。</param>
            /// <param name="disposeStack">释放的调用堆栈。</param>
            [Event(2, Level = EventLevel.Verbose, Version = 3)]
            public void MemoryStreamDisposed(Guid guid, string tag, long lifetimeMs, string allocationStack, string disposeStack)
            {
                if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
                {
                    WriteEvent(2, guid, tag ?? string.Empty, lifetimeMs, allocationStack ?? string.Empty, disposeStack ?? string.Empty);
                }
            }

            /// <summary>
            /// 当流第二次被释放时记录的事件。
            /// </summary>
            /// <param name="guid">此流的唯一 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="allocationStack">初始分配的调用堆栈。</param>
            /// <param name="disposeStack1">第一次释放的调用堆栈。</param>
            /// <param name="disposeStack2">第二次释放的调用堆栈。</param>
            /// <remarks>注意：只有在 RecyclableMemoryStreamManager.GenerateCallStacks 为 true 时，堆栈才会被填充。</remarks>
            [Event(3, Level = EventLevel.Critical)]
            public void MemoryStreamDoubleDispose(Guid guid, string tag, string allocationStack, string disposeStack1,
                                                  string disposeStack2)
            {
                if (this.IsEnabled())
                {
                    this.WriteEvent(3, guid, tag ?? string.Empty, allocationStack ?? string.Empty,
                                    disposeStack1 ?? string.Empty, disposeStack2 ?? string.Empty);
                }
            }

            /// <summary>
            /// 当流被终结时记录的事件。
            /// </summary>
            /// <param name="guid">此流的唯一 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="allocationStack">初始分配的调用堆栈。</param>
            /// <remarks>注意：只有在 RecyclableMemoryStreamManager.GenerateCallStacks 为 true 时，堆栈才会被填充。</remarks>
            [Event(4, Level = EventLevel.Error)]
            public void MemoryStreamFinalized(Guid guid, string tag, string allocationStack)
            {
                if (this.IsEnabled())
                {
                    WriteEvent(4, guid, tag ?? string.Empty, allocationStack ?? string.Empty);
                }
            }

            /// <summary>
            /// 当流的 ToArray 方法被调用时记录的事件。
            /// </summary>
            /// <param name="guid">此流的唯一 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="stack">ToArray 方法的调用堆栈。</param>
            /// <param name="size">流的长度。</param>
            /// <remarks>注意：只有在 RecyclableMemoryStreamManager.GenerateCallStacks 为 true 时，堆栈才会被填充。</remarks>
            [Event(5, Level = EventLevel.Verbose, Version = 2)]
            public void MemoryStreamToArray(Guid guid, string tag, string stack, long size)
            {
                if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
                {
                    WriteEvent(5, guid, tag ?? string.Empty, stack ?? string.Empty, size);
                }
            }

            /// <summary>
            /// 当 RecyclableMemoryStreamManager 被初始化时记录的事件。
            /// </summary>
            /// <param name="blockSize">块的大小，以字节为单位。</param>
            /// <param name="largeBufferMultiple">大缓冲区的倍数，以字节为单位。</param>
            /// <param name="maximumBufferSize">最大缓冲区大小，以字节为单位。</param>
            [Event(6, Level = EventLevel.Informational)]
            public void MemoryStreamManagerInitialized(int blockSize, int largeBufferMultiple, int maximumBufferSize)
            {
                if (this.IsEnabled())
                {
                    WriteEvent(6, blockSize, largeBufferMultiple, maximumBufferSize);
                }
            }

            /// <summary>
            /// 当创建新的块时记录的事件。
            /// </summary>
            /// <param name="smallPoolInUseBytes">当前在小块池中使用的字节数。</param>
            [Event(7, Level = EventLevel.Warning, Version = 2)]
            public void MemoryStreamNewBlockCreated(long smallPoolInUseBytes)
            {
                if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
                {
                    WriteEvent(7, smallPoolInUseBytes);
                }
            }

            /// <summary>
            /// 当创建新的大缓冲区时记录的事件。
            /// </summary>
            /// <param name="requiredSize">请求的大小。</param>
            /// <param name="largePoolInUseBytes">当前在大缓冲区池中使用的字节数。</param>
            [Event(8, Level = EventLevel.Warning, Version = 3)]
            public void MemoryStreamNewLargeBufferCreated(long requiredSize, long largePoolInUseBytes)
            {
                if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
                {
                    WriteEvent(8, requiredSize, largePoolInUseBytes);
                }
            }

            /// <summary>
            /// 当创建的缓冲区过大无法放入池中时记录的事件。
            /// </summary>
            /// <param name="guid">唯一的流 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="requiredSize">调用者请求的大小。</param>
            /// <param name="allocationStack">请求流的调用堆栈。</param>
            /// <remarks>注意：只有在 RecyclableMemoryStreamManager.GenerateCallStacks 为 true 时，堆栈才会被填充。</remarks>
            [Event(9, Level = EventLevel.Verbose, Version = 3)]
            public void MemoryStreamNonPooledLargeBufferCreated(Guid guid, string tag, long requiredSize, string allocationStack)
            {
                if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
                {
                    WriteEvent(9, guid, tag ?? string.Empty, requiredSize, allocationStack ?? string.Empty);
                }
            }

            /// <summary>
            /// 当缓冲区被丢弃时记录的事件（没有放回池中，而是交由 GC 清理）。
            /// </summary>
            /// <param name="guid">唯一的流 ID。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="bufferType">被丢弃的缓冲区类型。</param>
            /// <param name="reason">丢弃原因。</param>
            /// <param name="smallBlocksFree">小块池中的空闲块数。</param>
            /// <param name="smallPoolBytesFree">小块池中的空闲字节数。</param>
            /// <param name="smallPoolBytesInUse">从小块池中使用的字节数。</param>
            /// <param name="largeBlocksFree">大缓冲区池中的空闲块数。</param>
            /// <param name="largePoolBytesFree">大缓冲区池中的空闲字节数。</param>
            /// <param name="largePoolBytesInUse">从大缓冲区池中使用的字节数。</param>
            [Event(10, Level = EventLevel.Warning, Version = 2)]
            public void MemoryStreamDiscardBuffer(Guid guid, string tag, MemoryStreamBufferType bufferType,
                                                  MemoryStreamDiscardReason reason, long smallBlocksFree, long smallPoolBytesFree, long smallPoolBytesInUse, long largeBlocksFree, long largePoolBytesFree, long largePoolBytesInUse)
            {
                if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
                {
                    WriteEvent(10, guid, tag ?? string.Empty, bufferType, reason, smallBlocksFree, smallPoolBytesFree, smallPoolBytesInUse, largeBlocksFree, largePoolBytesFree, largePoolBytesInUse);
                }
            }

            /// <summary>
            /// 当流的容量超过最大值时记录的事件。
            /// </summary>
            /// <param name="guid">唯一的流 ID。</param>
            /// <param name="requestedCapacity">请求的容量。</param>
            /// <param name="maxCapacity">最大容量，由 RecyclableMemoryStreamManager 配置。</param>
            /// <param name="tag">临时 ID，通常表示当前使用情况。</param>
            /// <param name="allocationStack">容量请求的调用堆栈。</param>
            /// <remarks>注意：只有在 RecyclableMemoryStreamManager.GenerateCallStacks 为 true 时，堆栈才会被填充。</remarks>
            [Event(11, Level = EventLevel.Error, Version = 3)]
            public void MemoryStreamOverCapacity(Guid guid, string tag, long requestedCapacity, long maxCapacity, string allocationStack)
            {
                if (this.IsEnabled())
                {
                    WriteEvent(11, guid, tag ?? string.Empty, requestedCapacity, maxCapacity, allocationStack ?? string.Empty);
                }
            }
        }
    }
}
