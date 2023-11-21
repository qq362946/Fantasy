using System.Net.Sockets;

namespace Fantasy
{
    /// <summary>
    /// 提供扩展方法以操作 Socket 缓冲区大小。
    /// </summary>
    public static class SocketExtensions
    {
        /// <summary>
        /// 将 Socket 缓冲区大小设置为操作系统限制。
        /// </summary>
        /// <param name="socket">要设置缓冲区大小的 Socket。</param>
        public static void SetSocketBufferToOsLimit(this Socket socket)
        {
            socket.SetReceiveBufferToOSLimit();
            socket.SetSendBufferToOSLimit();
        }

        /// <summary>
        /// 将 Socket 接收缓冲区大小设置为操作系统限制。
        /// 尝试增加接收缓冲区大小的次数 = 默认 + 最大增加 100 MB。
        /// </summary>
        /// <param name="socket">要设置接收缓冲区大小的 Socket。</param>
        /// <param name="stepSize">每次增加的步长大小。</param>
        /// <param name="attempts">尝试增加缓冲区大小的次数。</param>
        public static void SetReceiveBufferToOSLimit(this Socket socket, int stepSize = 1024, int attempts = 100_000)
        {
            // setting a too large size throws a socket exception.
            // so let's keep increasing until we encounter it.
            for (int i = 0; i < attempts; ++i)
            {
                // increase in 1 KB steps
                try
                {
                    socket.ReceiveBufferSize += stepSize;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 将 Socket 发送缓冲区大小设置为操作系统限制。
        /// 尝试增加发送缓冲区大小的次数 = 默认 + 最大增加 100 MB。
        /// </summary>
        /// <param name="socket">要设置发送缓冲区大小的 Socket。</param>
        /// <param name="stepSize">每次增加的步长大小。</param>
        /// <param name="attempts">尝试增加缓冲区大小的次数。</param>
        public static void SetSendBufferToOSLimit(this Socket socket, int stepSize = 1024, int attempts = 100_000)
        {
            // setting a too large size throws a socket exception.
            // so let's keep increasing until we encounter it.
            for (var i = 0; i < attempts; ++i)
            {
                // increase in 1 KB steps
                try
                {
                    socket.SendBufferSize += stepSize;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }
    }
}