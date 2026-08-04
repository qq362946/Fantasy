using System;
using System.Linq;
using System.Security.Cryptography;

namespace Fantasy.Network.Security
{
    /// <summary>
    /// 传输层加密：X25519 + AES-256-CBC + HMAC-SHA256。加密帧: [IV:16][密文][HMAC:32]
    /// </summary>
    public sealed class EncryptionHelper
    {
        // 只保存密钥，每次 Encrypt/Decrypt 创建独立的 Aes/HMACSHA256 实例，因此并发调用无需加锁。
        // 密钥在 DeriveSharedKey 一次性写入后不再修改，配合 volatile 的 _isReady 保证跨线程可见性。
        private byte[] _aesKey = null!;
        private byte[] _hmacKey = null!;
        private volatile bool _isReady;

        public bool IsReady => _isReady;
        public byte[] PublicKey { get; private set; } = null!;
        public byte[] PrivateKey { get; private set; } = null!;
        public const int Overhead = 64;
        public const int PublicKeySize = 32;
        public const byte KeyExchangeMarker = 0xEC;
        public const int KeyExchangeFrameSize = 33;

        public void GenerateKeyPair()
        {
            var kp = X25519KeyAgreement.GenerateKeyPair();
            PrivateKey = kp.PrivateKey;
            PublicKey = kp.PublicKey;
        }

        public void DeriveSharedKey(ReadOnlySpan<byte> peerPublicKey)
        {
            var sharedSecret = X25519KeyAgreement.Agreement(PrivateKey, peerPublicKey.ToArray());
            using var sha = SHA256.Create();
            _aesKey = sha.ComputeHash(sharedSecret.Concat(new byte[] { 0x01 }).ToArray());
            _hmacKey = sha.ComputeHash(sharedSecret.Concat(new byte[] { 0x02 }).ToArray());
            _isReady = true;
        }

        /// <summary>
        /// 分配加解密缓冲区
        /// </summary>
        /// <param name="bufferSize">缓冲区大小，由调用方按协议需求指定（TCP 用 MaxMessageSize + Overhead + 4，KCP 用 Mtu + Overhead）</param>
        public static (byte[] encrypt, byte[] decrypt) CreateBuffers(int bufferSize)
        {
            return (new byte[bufferSize], new byte[bufferSize]);
        }

        /// <summary>
        /// 按最大消息大小分配加解密缓冲区（含 4 字节长度前缀头的空间）
        /// </summary>
        public static (byte[] encrypt, byte[] decrypt) CreateBuffers()
        {
            return CreateBuffers(ProgramDefine.MaxMessageSize + Overhead + sizeof(int));
        }

        public int Encrypt(ReadOnlySpan<byte> src, int srcOffset, int srcCount, Span<byte> dst, int dstOffset)
        {
            Span<byte> iv = stackalloc byte[16];
            RandomNumberGenerator.Fill(iv);
            iv.CopyTo(dst.Slice(dstOffset, 16));

            int cipherLen = EncryptData(src.Slice(srcOffset, srcCount), iv, dst.Slice(dstOffset + 16));

            int total = 16 + cipherLen;
            Span<byte> hashBuf = stackalloc byte[32];
            using (var hmac = new HMACSHA256(_hmacKey))
            {
                hmac.TryComputeHash(dst.Slice(dstOffset, total), hashBuf, out _);
            }
            hashBuf.CopyTo(dst.Slice(dstOffset + total, 32));
            return total + 32;
        }

        public int Decrypt(ReadOnlySpan<byte> src, int srcOffset, int srcCount, Span<byte> dst, int dstOffset)
        {
            if (srcCount < 48)
                throw new CryptographicException("Encrypted data too short");

            int bodyLen = srcCount - 32;
            Span<byte> hashExpected = stackalloc byte[32];
            using (var hmac = new HMACSHA256(_hmacKey))
            {
                hmac.TryComputeHash(src.Slice(srcOffset, bodyLen), hashExpected, out _);
            }

            if (!ConstantTimeEqual(hashExpected, src.Slice(srcOffset + bodyLen, 32)))
                throw new CryptographicException("HMAC verification failed");

            return DecryptData(
                src.Slice(srcOffset + 16, srcCount - 48),
                src.Slice(srcOffset, 16),
                dst.Slice(dstOffset));
        }

#if FANTASY_NET || FANTASY_CONSOLE
        private int EncryptData(ReadOnlySpan<byte> src, ReadOnlySpan<byte> iv, Span<byte> dst)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            return aes.EncryptCbc(src, iv, dst, PaddingMode.PKCS7);
        }

        private int DecryptData(ReadOnlySpan<byte> src, ReadOnlySpan<byte> iv, Span<byte> dst)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            return aes.DecryptCbc(src, iv, dst, PaddingMode.PKCS7);
        }
#else
        private int EncryptData(ReadOnlySpan<byte> src, ReadOnlySpan<byte> iv, Span<byte> dst)
        {
            using var aes = Aes.Create();
            using var enc = aes.CreateEncryptor(_aesKey, iv.ToArray());
            var r = enc.TransformFinalBlock(src.ToArray(), 0, src.Length);
            r.CopyTo(dst);
            return r.Length;
        }

        private int DecryptData(ReadOnlySpan<byte> src, ReadOnlySpan<byte> iv, Span<byte> dst)
        {
            using var aes = Aes.Create();
            using var dec = aes.CreateDecryptor(_aesKey, iv.ToArray());
            var r = dec.TransformFinalBlock(src.ToArray(), 0, src.Length);
            r.CopyTo(dst);
            return r.Length;
        }
#endif

        private static bool ConstantTimeEqual(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
