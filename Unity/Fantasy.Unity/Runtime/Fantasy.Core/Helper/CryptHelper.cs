using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fantasy.Helper
{
    /// <summary>
    /// 提供加密和解密相关的辅助方法。
    /// </summary>
    public static class CryptHelper
    {
        // 对应的字符串SYUwQN360OPY1gaL
        // 在这个网站随机生成的 http://tool.c7sky.com/password/
        private static readonly byte[] Key =
        {
            0x53, 0x59, 0x55, 0x77, 0x51, 0x4e, 0x33, 0x36,
            0x30, 0x4f, 0x50, 0x59, 0x31, 0x67, 0x61, 0x4c
        };

        /// <summary>
        /// 创建一个新的加密密钥
        /// </summary>
        /// <param name="keyStr">一个长度为16的字符串、如果超过只会截取前16位</param>
        /// <returns>返回的是一个十六进制的字符串、每个用,分割的、每个都是一个byte</returns>
        public static string CreateKey(string keyStr)
        {
            if (keyStr.Length > 16)
            {
                keyStr = keyStr.Substring(0, 15);
            }

            var bytes = Encoding.UTF8.GetBytes(keyStr);
            return $"0x{BitConverter.ToString(bytes, 0).Replace("-", ", 0x").ToLower()}";
        }

        /// <summary>
        /// 使用 AES 算法对字节数组进行加密。
        /// </summary>
        /// <param name="toEncryptArray">要加密的字节数组。</param>
        /// <returns>加密后的字节数组。</returns>
        public static byte[] AesEncrypt(byte[] toEncryptArray)
        {
            var rm = Aes.Create();
            rm.Key = Key;
            rm.Mode = CipherMode.ECB;
            rm.Padding = PaddingMode.PKCS7;
            var cTransform = rm.CreateEncryptor();
            return cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        }

        /// <summary>
        /// 使用 AES 算法对字节数组进行解密，返回一个内存流。
        /// </summary>
        /// <param name="toEncryptArray">要解密的字节数组。</param>
        /// <returns>解密后的内存流。</returns>
        public static MemoryStream AesDecryptReturnStream(byte[] toEncryptArray)
        {
            var bytes = AesDecrypt(toEncryptArray);

            return new MemoryStream(bytes);
        }

        /// <summary>
        /// 使用 AES 算法对字节数组进行解密。
        /// </summary>
        /// <param name="toEncryptArray">要解密的字节数组。</param>
        /// <returns>解密后的字节数组。</returns>
        public static byte[] AesDecrypt(byte[] toEncryptArray)
        {
            var rm = Aes.Create();
            rm.Key = Key;
            rm.Mode = CipherMode.ECB;
            rm.Padding = PaddingMode.PKCS7;

            var cTransform = rm.CreateDecryptor();
            return cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        }
    }
}