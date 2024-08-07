using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fantasy
{
    /// <summary>
    /// 提供计算 MD5 散列值的辅助方法。
    /// </summary>
    public static class EncryptHelper
    {
        private static readonly SHA256 _sha256Hash = SHA256.Create();
        
        /// <summary>
        /// 计算指定字节数组的Sha256。
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ComputeSha256Hash(byte[] bytes)
        {
#if FANTASY_UNITY
            using var sha256Hash = SHA256.Create();
            return sha256Hash.ComputeHash(bytes);
#else
            return SHA256.HashData(bytes);
#endif
        }

        /// <summary>
        /// 计算指定文件的 MD5 散列值。
        /// </summary>
        /// <param name="filePath">要计算散列值的文件路径。</param>
        /// <returns>表示文件的 MD5 散列值的字符串。</returns>
        public static string FileMD5(string filePath)
        {
            using var file = new FileStream(filePath, FileMode.Open);
            return FileMD5(file);
        }

        /// <summary>
        /// 计算给定文件流的 MD5 散列值。
        /// </summary>
        /// <param name="fileStream">要计算散列值的文件流。</param>
        /// <returns>表示文件流的 MD5 散列值的字符串。</returns>
        public static string FileMD5(FileStream fileStream)
        {
            var md5 = MD5.Create();
            return md5.ComputeHash(fileStream).ToHex("x2");
        }

        /// <summary>
        /// 计算给定字节数组的 MD5 散列值。
        /// </summary>
        /// <param name="bytes">要计算散列值的字节数组。</param>
        /// <returns>表示字节数组的 MD5 散列值的字符串。</returns>
        public static string BytesMD5(byte[] bytes)
        {
            var md5 = MD5.Create();
            bytes = md5.ComputeHash(bytes);
            return bytes.ToHex("x2");
        }

        /// <summary>
        /// 根据字符串计算一个Hash值
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static int ComputeSha256HashAsInt(string rawData)
        {
            var bytes = _sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }
    }
}