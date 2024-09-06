using System;
using System.Buffers;
using System.IO;

namespace Fantasy
{
    public sealed class MemoryStreamBuffer : MemoryStream, IBufferWriter<byte>
    {
        public MemoryStreamBuffer() { }
        public MemoryStreamBuffer(int capacity): base(capacity) { }
        public MemoryStreamBuffer(byte[] buffer): base(buffer) { } 
        
        public void Advance(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "The value of 'count' cannot be negative.");
            }
            
            var newLength = Position + count;
            if (newLength > Length)
            {
                SetLength(newLength);
            }
            Position = newLength;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), sizeHint, "The value of 'count' cannot be negative.");
            }

            if (Length - Position <= sizeHint)
            {
                SetLength(Position + sizeHint);
            }
            
            return new Memory<byte>(GetBuffer(), (int)Position, (int)(Length - Position));
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), sizeHint, "The value of 'count' cannot be negative.");
            }
            
            if (Length - Position <= sizeHint)
            {
                SetLength(Position + sizeHint);
            }
            
            return new Span<byte>(GetBuffer(), (int)Position, (int)(Length - Position));
        }
    }
}