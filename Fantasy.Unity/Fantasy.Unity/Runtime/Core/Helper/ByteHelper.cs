using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Fantasy
{
    /// <summary>
    /// 提供字节操作辅助方法的静态类。
    /// </summary>
    public static class ByteHelper
    {
        private static readonly string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };

        /// <summary>
        /// 从指定的文件流中读取一个 64 位整数。
        /// </summary>
        public static long ReadInt64(FileStream stream)
        {
            var buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// 从指定的文件流中读取一个 32 位整数。
        /// </summary>
        public static int ReadInt32(FileStream stream)
        {
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// 从指定的内存流中读取一个 64 位整数。
        /// </summary>
        public static long ReadInt64(MemoryStream stream)
        {
            var buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// 从指定的内存流中读取一个 32 位整数。
        /// </summary>
        public static int ReadInt32(MemoryStream stream)
        {
            var buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// 将字节转换为十六进制字符串表示。
        /// </summary>
        public static string ToHex(this byte b)
        {
            return b.ToString("X2");
        }

        /// <summary>
        /// 将字节数组转换为十六进制字符串表示。
        /// </summary>
        public static string ToHex(this byte[] bytes)
        {
            var stringBuilder = new StringBuilder();
            foreach (var b in bytes)
            {
                stringBuilder.Append(b.ToString("X2"));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 将字节数组按指定格式转换为十六进制字符串表示。
        /// </summary>
        public static string ToHex(this byte[] bytes, string format)
        {
            var stringBuilder = new StringBuilder();
            foreach (var b in bytes)
            {
                stringBuilder.Append(b.ToString(format));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 将字节数组的指定范围按十六进制格式转换为字符串表示。
        /// </summary>
        public static string ToHex(this byte[] bytes, int offset, int count)
        {
            var stringBuilder = new StringBuilder();
            for (var i = offset; i < offset + count; ++i)
            {
                stringBuilder.Append(bytes[i].ToString("X2"));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 将字节数组转换为默认编码的字符串表示。
        /// </summary>
        public static string ToStr(this byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

        /// <summary>
        /// 将字节数组的指定范围按默认编码转换为字符串表示。
        /// </summary>
        public static string ToStr(this byte[] bytes, int index, int count)
        {
            return Encoding.Default.GetString(bytes, index, count);
        }

        /// <summary>
        /// 将字节数组转换为 UTF-8 编码的字符串表示。
        /// </summary>
        public static string Utf8ToStr(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 将字节数组的指定范围按 UTF-8 编码转换为字符串表示。
        /// </summary>
        public static string Utf8ToStr(this byte[] bytes, int index, int count)
        {
            return Encoding.UTF8.GetString(bytes, index, count);
        }

        /// <summary>
        /// 将无符号整数写入字节数组的指定偏移位置。
        /// </summary>
        public static void WriteTo(this byte[] bytes, int offset, uint num)
        {
            bytes[offset]     = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num & 0xff00)     >> 8);
            bytes[offset + 2] = (byte)((num & 0xff0000)   >> 16);
            bytes[offset + 3] = (byte)((num & 0xff000000) >> 24);
        }

        /// <summary>
        /// 将有符号整数写入字节数组的指定偏移位置。
        /// </summary>
        public static void WriteTo(this byte[] bytes, int offset, int num)
        {
            bytes[offset]     = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num & 0xff00)     >> 8);
            bytes[offset + 2] = (byte)((num & 0xff0000)   >> 16);
            bytes[offset + 3] = (byte)((num & 0xff000000) >> 24);
        }

        /// <summary>
        /// 将字节写入字节数组的指定偏移位置。
        /// </summary>
        public static void WriteTo(this byte[] bytes, int offset, byte num)
        {
            bytes[offset] = num;
        }

        /// <summary>
        /// 将有符号短整数写入字节数组的指定偏移位置。
        /// </summary>
        public static void WriteTo(this byte[] bytes, int offset, short num)
        {
            bytes[offset] = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num & 0xff00) >> 8);
        }

        /// <summary>
        /// 将无符号短整数写入字节数组的指定偏移位置。
        /// </summary>
        public static void WriteTo(this byte[] bytes, int offset, ushort num)
        {
            bytes[offset]     = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num & 0xff00) >> 8);
        }

        /// <summary>
        /// 将字节数转换为可读的速度表示。
        /// </summary>
        /// <param name="byteCount">字节数</param>
        /// <returns>可读的速度表示</returns>
        public static string ToReadableSpeed(this long byteCount)
        {
            var    i        = 0;
            double dblSByte = byteCount;
            if (byteCount <= 1024)
            {
                return $"{dblSByte:0.##}{Suffix[i]}";
            }

            for (i = 0; byteCount / 1024 > 0; i++, byteCount /= 1024)
            {
                dblSByte = byteCount / 1024.0;
            }

            return $"{dblSByte:0.##}{Suffix[i]}";
        }

        /// <summary>
        /// 将字节数转换为可读的速度表示。
        /// </summary>
        /// <param name="byteCount">字节数</param>
        /// <returns>可读的速度表示</returns>
        public static string ToReadableSpeed(this ulong byteCount)
        {
            var i = 0;
            double dblSByte = byteCount;

            if (byteCount <= 1024)
            {
                return $"{dblSByte:0.##}{Suffix[i]}";
            }

            for (i = 0; byteCount / 1024 > 0; i++, byteCount /= 1024)
            {
                dblSByte = byteCount / 1024.0;
            }

            return $"{dblSByte:0.##}{Suffix[i]}";
        }

        /// <summary>
        /// 合并两个字节数组。
        /// </summary>
        /// <param name="bytes">第一个字节数组</param>
        /// <param name="otherBytes">第二个字节数组</param>
        /// <returns>合并后的字节数组</returns>
        public static byte[] MergeBytes(byte[] bytes, byte[] otherBytes)
        {
            var result = new byte[bytes.Length + otherBytes.Length];
            bytes.CopyTo(result, 0);
            otherBytes.CopyTo(result, bytes.Length);
            return result;
        }

        /// <summary>
        /// 根据int值获取字节数组。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(this int value, byte[] buffer)
        {
            if (buffer.Length < 4)
            {
                throw new ArgumentException("Buffer too small.");
            }
                
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
        }

        /// <summary>
        /// 根据int值获取字节数组。
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this MemoryStream memoryStream, int value)
        {
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(4);
            var memorySpan = memoryOwner.Memory.Span;
            memorySpan[0] = (byte)value;
            memorySpan[1] = (byte)(value >> 8);
            memorySpan[2] = (byte)(value >> 16);
            memorySpan[3] = (byte)(value >> 24);
            memoryStream.Write(memorySpan);
        }

        /// <summary>
        /// 根据uint值获取字节数组。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(ref this uint value, byte[] buffer)
        {
            if (buffer.Length < 4)
            {
                throw new ArgumentException("Buffer too small.");
            }
                
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
        }
        
        /// <summary>
        /// 根据uint值获取字节数组。
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this MemoryStream memoryStream, uint value)
        {
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(4);
            var memorySpan = memoryOwner.Memory.Span;
            memorySpan[0] = (byte)value;
            memorySpan[1] = (byte)(value >> 8);
            memorySpan[2] = (byte)(value >> 16);
            memorySpan[3] = (byte)(value >> 24);
            memoryStream.Write(memorySpan);
        }
        
        /// <summary>
        /// 根据int值获取字节数组。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="buffer"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBytes(this long value, byte[] buffer)
        {
            if (buffer.Length < 8)
            {
                throw new ArgumentException("Buffer too small.");
            }
                
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
        }
        
        /// <summary>
        /// 根据uint值获取字节数组。
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this MemoryStream memoryStream, long value)
        {
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(8);
            var memorySpan = memoryOwner.Memory.Span;
            memorySpan[0] = (byte)value;
            memorySpan[1] = (byte)(value >> 8);
            memorySpan[2] = (byte)(value >> 16);
            memorySpan[3] = (byte)(value >> 24);
            memorySpan[4] = (byte)(value >> 32);
            memorySpan[5] = (byte)(value >> 40);
            memorySpan[6] = (byte)(value >> 48);
            memorySpan[7] = (byte)(value >> 56);
            memoryStream.Write(memorySpan);
        }
    }
}