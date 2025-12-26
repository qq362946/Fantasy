/*
  FastLZ - Byte-aligned LZ77 compression library
  Copyright (C) 2005-2020 Ariya Hidayat <ariya.hidayat@gmail.com>
  Copyright (C) 2023 Choi Ikpil <ikpil@naver.com> https://github.com/ikpil/DotFastLZ

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/

using System;

namespace DotRecast.Core.Compression
{
    public static class FastLZ
    {
        public const int FASTLZ_VERSION = 0x000500;
        public const int FASTLZ_VERSION_MAJOR = 0;
        public const int FASTLZ_VERSION_MINOR = 5;
        public const int FASTLZ_VERSION_REVISION = 0;
        public const string FASTLZ_VERSION_STRING = "0.5.0";

        public const int MAX_COPY = 32;
        public const int MAX_LEN = 264; /* 256 + 8 */
        public const int MAX_L1_DISTANCE = 8192;
        public const int MAX_L2_DISTANCE = 8191;
        public const int MAX_FARDISTANCE = (65535 + MAX_L2_DISTANCE - 1);
        public const int HASH_LOG = 13;
        public const int HASH_SIZE = (1 << HASH_LOG);
        public const int HASH_MASK = (HASH_SIZE - 1);

        /**
          Compress a block of data in the input buffer and returns the size of
          compressed block. The size of input buffer is specified by length. The
          minimum input buffer size is 16.

          The output buffer must be at least 5% larger than the input buffer
          and can not be smaller than 66 bytes.

          If the input is not compressible, the return value might be larger than
          length (input buffer size).

          The input buffer and the output buffer can not overlap.

          Compression level can be specified in parameter level. At the moment,
          only level 1 and level 2 are supported.
          Level 1 is the fastest compression and generally useful for short data.
          Level 2 is slightly slower but it gives better compression ratio.

          Note that the compressed data, regardless of the level, can always be
          decompressed using the function fastlz_decompress below.
        */
        // fastlz_compress
        public static long CompressLevel(int level, byte[] input, long length, byte[] output)
        {
            return CompressLevel(level, input, 0, length, output);
        }

        public static long CompressLevel(int level, byte[] input, long inputOffset, long length, byte[] output)
        {
            if (level == 1)
            {
                return CompressLevel1(input, inputOffset, length, output);
            }

            if (level == 2)
            {
                return CompressLevel2(input, inputOffset, length, output);
            }

            throw new Exception($"invalid level: {level} (expected: 1 or 2)");
        }

        // fastlz1_compress
        public static long CompressLevel1(byte[] input, long inputOffset, long length, byte[] output)
        {
            long ip = inputOffset;
            long ip_start = ip;
            long ip_bound = ip + length - 4;
            long ip_limit = ip + length - 12 - 1;

            long op = 0;

            long[] htab = new long[HASH_SIZE];
            long seq, hash;

            // Initializes hash table
            for (hash = 0; hash < HASH_SIZE; ++hash)
            {
                htab[hash] = 0;
            }

            // We start with literal copy
            long anchor = ip;
            ip += 2;

            // Main loop
            while (ip < ip_limit)
            {
                long refIdx;
                long distance, cmp;

                // Find potential match
                do
                {
                    seq = ReadUInt32(input, ip) & 0xffffff;
                    hash = Hash(seq);
                    refIdx = ip_start + htab[hash];
                    htab[hash] = ip - ip_start;
                    distance = ip - refIdx;
                    cmp = distance < MAX_L1_DISTANCE
                        ? ReadUInt32(input, refIdx) & 0xffffff
                        : 0x1000000;

                    if (ip >= ip_limit)
                    {
                        break;
                    }

                    ++ip;
                } while (seq != cmp);

                if (ip >= ip_limit)
                {
                    break;
                }

                --ip;

                if (ip > anchor)
                {
                    op = Literals(ip - anchor, input, anchor, output, op);
                }

                long len = MemCompare(input, refIdx + 3, input, ip + 3, ip_bound);
                op = MatchLevel1(len, distance, output, op);

                // Update the hash at the match boundary
                ip += len;
                seq = ReadUInt32(input, ip);
                hash = Hash(seq & 0xffffff);
                htab[hash] = ip++ - ip_start;
                seq >>= 8;
                hash = Hash(seq);
                htab[hash] = ip++ - ip_start;

                anchor = ip;
            }

            long copy = length - anchor;
            op = Literals(copy, input, anchor, output, op);
            return op;
        }

        // fastlz2_compress
        public static long CompressLevel2(byte[] input, long inputOffset, long length, byte[] output)
        {
            long ip = inputOffset;
            long ip_start = ip;
            long ip_bound = ip + length - 4; /* because readU32 */
            long ip_limit = ip + length - 12 - 1;

            long op = 0;

            long[] htab = new long[HASH_SIZE];
            long seq, hash;

            /* initializes hash table */
            for (hash = 0; hash < HASH_SIZE; ++hash)
            {
                htab[hash] = 0;
            }

            /* we start with literal copy */
            long anchor = ip;
            ip += 2;

            /* main loop */
            while (ip < ip_limit)
            {
                long refs;
                long distance, cmp;

                /* find potential match */
                do
                {
                    seq = ReadUInt32(input, ip) & 0xffffff;
                    hash = Hash(seq);
                    refs = ip_start + htab[hash];
                    htab[hash] = ip - ip_start;
                    distance = ip - refs;
                    cmp = distance < MAX_FARDISTANCE
                        ? ReadUInt32(input, refs) & 0xffffff
                        : 0x1000000;

                    if (ip >= ip_limit)
                    {
                        break;
                    }

                    ++ip;
                } while (seq != cmp);

                if (ip >= ip_limit)
                {
                    break;
                }

                --ip;

                /* far, needs at least 5-byte match */
                if (distance >= MAX_L2_DISTANCE)
                {
                    if (input[refs + 3] != input[ip + 3] || input[refs + 4] != input[ip + 4])
                    {
                        ++ip;
                        continue;
                    }
                }

                if (ip > anchor)
                {
                    op = Literals(ip - anchor, input, anchor, output, op);
                }

                long len = MemCompare(input, refs + 3, input, ip + 3, ip_bound);
                op = MatchLevel2(len, distance, output, op);

                /* update the hash at match boundary */
                ip += len;
                seq = ReadUInt32(input, ip);
                hash = Hash(seq & 0xffffff);
                htab[hash] = ip++ - ip_start;
                seq >>= 8;
                hash = Hash(seq);
                htab[hash] = ip++ - ip_start;

                anchor = ip;
            }

            long copy = length - anchor;
            op = Literals(copy, input, anchor, output, op);

            /* marker for fastlz2 */
            output[inputOffset] |= (1 << 5);

            return op;
        }

        /**
          Decompress a block of compressed data and returns the size of the
          decompressed block. If error occurs, e.g. the compressed data is
          corrupted or the output buffer is not large enough, then 0 (zero)
          will be returned instead.

          The input buffer and the output buffer can not overlap.

          Decompression is memory safe and guaranteed not to write the output buffer
          more than what is specified in maxout.

          Note that the decompression will always work, regardless of the
          compression level specified in fastlz_compress_level above (when
          producing the compressed block).
         */
        // fastlz_decompress
        public static long Decompress(byte[] input, long length, byte[] output, long maxout)
        {
            return Decompress(input, 0, length, output, 0, maxout);
        }

        public static long Decompress(byte[] input, long inputOffset, long length, byte[] output, long outputOffset, long maxout)
        {
            /* magic identifier for compression level */
            int level = (input[inputOffset] >> 5) + 1;

            if (level == 1)
            {
                return DecompressLevel1(input, inputOffset, length, output, outputOffset, maxout);
            }

            if (level == 2)
            {
                return DecompressLevel2(input, inputOffset, length, output, outputOffset, maxout);
            }

            throw new Exception($"invalid level: {level} (expected: 1 or 2)");
        }

        // fastlz1_decompress
        public static long DecompressLevel1(byte[] input, long inputOffset, long length, byte[] output, long outputOffset, long maxout)
        {
            long ip = inputOffset;
            long ip_limit = ip + length;
            long ip_bound = ip_limit - 2;

            long op = outputOffset;
            long op_limit = op + maxout;
            long ctrl = input[ip++] & 31;

            while (true)
            {
                if (ctrl >= 32)
                {
                    long len = (ctrl >> 5) - 1;
                    long ofs = (ctrl & 31) << 8;
                    long refs = op - ofs - 1;
                    if (len == 7 - 1)
                    {
                        if (!(ip <= ip_bound))
                        {
                            return 0;
                        }

                        len += input[ip++];
                    }

                    refs -= input[ip++];
                    len += 3;
                    if (!(op + len <= op_limit))
                    {
                        return 0;
                    }

                    if (!(refs >= outputOffset))
                    {
                        return 0;
                    }

                    MemMove(output, op, output, refs, len);
                    op += len;
                }
                else
                {
                    ctrl++;
                    if (!(op + ctrl <= op_limit))
                    {
                        return 0;
                    }

                    if (!(ip + ctrl <= ip_limit))
                    {
                        return 0;
                    }

                    RcArrays.Copy(input, ip, output, op, ctrl);
                    ip += ctrl;
                    op += ctrl;
                }

                if (ip > ip_bound)
                {
                    break;
                }

                ctrl = input[ip++];
            }

            return op;
        }

        // fastlz2_decompress
        public static long DecompressLevel2(byte[] input, long inputOffset, long length, byte[] output, long outputOffset, long maxout)
        {
            long ip = inputOffset;
            long ip_limit = ip + length;
            long ip_bound = ip_limit - 2;

            long op = outputOffset;
            long op_limit = op + maxout;
            long ctrl = input[ip++] & 31;

            while (true)
            {
                if (ctrl >= 32)
                {
                    long len = (ctrl >> 5) - 1;
                    long ofs = (ctrl & 31) << 8;
                    long refIdx = op - ofs - 1;

                    long code;
                    if (len == 7 - 1)
                    {
                        do
                        {
                            if (!(ip <= ip_bound))
                            {
                                return 0;
                            }

                            code = input[ip++];
                            len += code;
                        } while (code == 255);
                    }

                    code = input[ip++];
                    refIdx -= code;
                    len += 3;

                    /* match from 16-bit distance */
                    if (code == 255)
                    {
                        if (ofs == (31 << 8))
                        {
                            if (!(ip < ip_bound))
                            {
                                return 0;
                            }

                            ofs = input[ip++] << 8;
                            ofs += input[ip++];
                            refIdx = op - ofs - MAX_L2_DISTANCE - 1;
                        }
                    }

                    if (!(op + len <= op_limit))
                    {
                        return 0;
                    }

                    if (!(refIdx >= outputOffset))
                    {
                        return 0;
                    }

                    MemMove(output, op, output, refIdx, len);
                    op += len;
                }
                else
                {
                    ctrl++;
                    if (!(op + ctrl <= op_limit))
                    {
                        return 0;
                    }

                    if (!(ip + ctrl <= ip_limit))
                    {
                        return 0;
                    }

                    RcArrays.Copy(input, ip, output, op, ctrl);
                    ip += ctrl;
                    op += ctrl;
                }

                if (ip >= ip_limit)
                {
                    break;
                }

                ctrl = input[ip++];
            }

            return op;
        }

        // flz_readu32
        public static uint ReadUInt32(byte[] data, long offset)
        {
            return ((uint)data[offset + 3] & 0xff) << 24 |
                   ((uint)data[offset + 2] & 0xff) << 16 |
                   ((uint)data[offset + 1] & 0xff) << 8 |
                   ((uint)data[offset + 0] & 0xff);
        }

        public static ushort ReadUInt16(byte[] data, long offset)
        {
            var u16 = ((uint)data[offset + 1] << 8) |
                      ((uint)data[offset + 0]);
            return (ushort)u16;
        }

        // flz_hash
        public static ushort Hash(long v)
        {
            ulong h = ((ulong)v * 2654435769UL) >> (32 - HASH_LOG);
            return (ushort)(h & HASH_MASK);
        }

        // special case of memcpy: at most MAX_COPY bytes
        // flz_smallcopy
        public static void SmallCopy(byte[] dest, long destOffset, byte[] src, long srcOffset, long count)
        {
            // if (count >= 4)
            // {
            //     count -= count % 4;
            //     RcArrays.Copy(src, srcOffset, dest, destOffset, count);
            // }
            RcArrays.Copy(src, srcOffset, dest, destOffset, count);
        }

        // special case of memcpy: exactly MAX_COPY bytes
        // flz_maxcopy
        static void MaxCopy(byte[] dest, long destOffset, byte[] src, long secOffset)
        {
            RcArrays.Copy(src, secOffset, dest, destOffset, MAX_COPY);
        }

        // flz_literals
        public static long Literals(long runs, byte[] src, long srcOffset, byte[] dest, long destOffset)
        {
            while (runs >= MAX_COPY)
            {
                dest[destOffset++] = MAX_COPY - 1;
                MaxCopy(dest, destOffset, src, srcOffset);
                srcOffset += MAX_COPY;
                destOffset += MAX_COPY;
                runs -= MAX_COPY;
            }

            if (runs > 0)
            {
                dest[destOffset++] = (byte)(runs - 1);
                SmallCopy(dest, destOffset, src, srcOffset, runs);
                destOffset += runs;
            }

            return destOffset;
        }

        // flz1_match
        public static long MatchLevel1(long len, long distance, byte[] output, long op)
        {
            --distance;
            if (len > MAX_LEN - 2)
            {
                while (len > MAX_LEN - 2)
                {
                    output[op++] = (byte)((7 << 5) + (distance >> 8));
                    output[op++] = (byte)(MAX_LEN - 2 - 7 - 2);
                    output[op++] = (byte)(distance & 255);
                    len -= MAX_LEN - 2;
                }
            }

            if (len < 7)
            {
                output[op++] = (byte)((len << 5) + (distance >> 8));
                output[op++] = (byte)(distance & 255);
            }
            else
            {
                output[op++] = (byte)((7 << 5) + (distance >> 8));
                output[op++] = (byte)(len - 7);
                output[op++] = (byte)((distance & 255));
            }

            return op;
        }

        // flz2_match
        public static long MatchLevel2(long len, long distance, byte[] output, long op)
        {
            --distance;
            if (distance < MAX_L2_DISTANCE)
            {
                if (len < 7)
                {
                    output[op++] = (byte)((len << 5) + (distance >> 8));
                    output[op++] = (byte)((distance & 255));
                }
                else
                {
                    output[op++] = (byte)((7 << 5) + (distance >> 8));
                    for (len -= 7; len >= 255; len -= 255)
                    {
                        output[op++] = 255;
                    }

                    output[op++] = (byte)(len);
                    output[op++] = (byte)((distance & 255));
                }
            }
            else
            {
                /* far away, but not yet in the another galaxy... */
                if (len < 7)
                {
                    distance -= MAX_L2_DISTANCE;
                    output[op++] = (byte)((len << 5) + 31);
                    output[op++] = (byte)(255);
                    output[op++] = (byte)(distance >> 8);
                    output[op++] = (byte)(distance & 255);
                }
                else
                {
                    distance -= MAX_L2_DISTANCE;
                    output[op++] = (7 << 5) + 31;
                    for (len -= 7; len >= 255; len -= 255)
                    {
                        output[op++] = 255;
                    }

                    output[op++] = (byte)(len);
                    output[op++] = (byte)(255);
                    output[op++] = (byte)(distance >> 8);
                    output[op++] = (byte)(distance & 255);
                }
            }

            return op;
        }

        // flz_cmp
        public static long MemCompare(byte[] p, long pOffset, byte[] q, long qOffset, long r)
        {
            long start = pOffset;

            if (4 <= r && ReadUInt32(p, pOffset) == ReadUInt32(q, qOffset))
            {
                pOffset += 4;
                qOffset += 4;
            }

            while (qOffset < r)
            {
                if (p[pOffset++] != q[qOffset++])
                    break;
            }

            return pOffset - start;
        }

        // fastlz_memmove
        public static void MemMove(byte[] dest, long destOffset, byte[] src, long srcOffset, long count)
        {
            if (dest.Length < destOffset + count)
            {
                throw new IndexOutOfRangeException($"{dest.Length} < {destOffset} + {count}");
            }

            if (src.Length < srcOffset + count)
            {
                throw new IndexOutOfRangeException($"{src.Length} < {srcOffset} + {count}");
            }

            for (long i = 0; i < count; ++i)
            {
                dest[destOffset + i] = src[srcOffset + i];
            }
        }

        public static long EstimateCompressedSize(long size)
        {
            long estimatedSize = (long)Math.Ceiling(size * 1.06f);
            return Math.Max(estimatedSize, 66);
        }
    }
}