#if !FANTASY_WEBGL
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable CS8603 // Possible null reference return.

// ReSharper disable InconsistentNaming

namespace Fantasy.Helper
{
    /// <summary>
    /// 提供网络操作相关的帮助方法。
    /// </summary>
    public static partial class NetworkHelper
    {
        /// <summary>
        /// 根据字符串获取一个IPEndPoint
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IPEndPoint GetIPEndPoint(string address)
        {
            try
            {
                var addressSplit = address.Split(':');
                if (addressSplit.Length != 2)
                {
                    throw new FormatException("Invalid format");
                }

                var ipString = addressSplit[0];
                var portString = addressSplit[1];

                if (!IPAddress.TryParse(ipString, out var ipAddress))
                {
                    throw new FormatException("Invalid IP address");
                }

                if (!int.TryParse(portString, out var port) || port < 0 || port > 65535)
                {
                    throw new FormatException("Invalid port number");
                }

                return new IPEndPoint(ipAddress, port);
            }
            catch (Exception e)
            {
                Log.Error($"Error parsing IP and Port:{e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 克隆一个IPEndPoint
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IPEndPoint Clone(this EndPoint endPoint)
        {
            var ip = (IPEndPoint)endPoint;
            return new IPEndPoint(ip.Address, ip.Port);
        }
        
        /// <summary>
        /// 比较两个IPEndPoint是否相等
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IPEndPointEquals(this EndPoint endPoint, IPEndPoint ipEndPoint)
        {
            var ip = (IPEndPoint)endPoint;
            return ip.Address.Equals(ipEndPoint.Address) && ip.Port == ipEndPoint.Port;
        }
        
        /// <summary>
        /// 比较两个IPEndPoint是否相等
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IPEndPointEquals(this IPEndPoint endPoint, IPEndPoint ipEndPoint)
        {
            return endPoint.Address.Equals(ipEndPoint.Address) && endPoint.Port == ipEndPoint.Port;
        }

#if !FANTASY_WEBGL
        /// <summary>
        /// 将SocketAddress写入到Byte[]中
        /// </summary>
        /// <param name="socketAddress"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void SocketAddressToByte(this SocketAddress socketAddress, byte[] buffer, int offset)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException(nameof(socketAddress), "The SocketAddress cannot be null.");
            }
            
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "The buffer cannot be null.");
            }

            if (buffer.Length < socketAddress.Size + offset + 8)
            {
                throw new ArgumentException("The buffer length is insufficient. It must be at least the size of the SocketAddress plus 8 bytes.", nameof(buffer));
            }
            
            fixed (byte* pBuffer = buffer)
            {
                var pOffsetBuffer = pBuffer + offset;
                var addressFamilyValue = (int)socketAddress.Family;
                var socketAddressSizeValue = socketAddress.Size;
                Buffer.MemoryCopy(&addressFamilyValue, pOffsetBuffer, buffer.Length - offset, sizeof(int));
                Buffer.MemoryCopy(&socketAddressSizeValue, pOffsetBuffer + 4, buffer.Length - offset -4, sizeof(int));
                for (var i = 0; i < socketAddress.Size - 2; i++)
                {
                    pOffsetBuffer[8 + i] = socketAddress[i + 2];
                }
            }
        }

        /// <summary>
        /// 将byre[]转换为SocketAddress
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="socketAddress"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ByteToSocketAddress(byte[] buffer, int offset, out SocketAddress socketAddress)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "The buffer cannot be null.");
            }
            
            if (buffer.Length < 8)
            {
                throw new ArgumentException("Buffer length is insufficient. It must be at least 8 bytes.", nameof(buffer));
            }
            
            try
            {
                fixed (byte* pBuffer = buffer)
                {
                    var pOffsetBuffer = pBuffer + offset;
                    var addressFamily = (AddressFamily)Marshal.ReadInt32((IntPtr)pOffsetBuffer);
                    var socketAddressSize = Marshal.ReadInt32((IntPtr)(pOffsetBuffer + 4));

                    if (buffer.Length < offset + 8 + socketAddressSize)
                    {
                        throw new ArgumentException("Buffer length is insufficient for the given SocketAddress size.", nameof(buffer));
                    }

                    socketAddress = new SocketAddress(addressFamily, socketAddressSize);
                    
                    for (var i = 0; i < socketAddressSize - 2; i++)
                    {
                        socketAddress[i + 2] = *(pOffsetBuffer + 8 + i);
                    }

                    return 8 + offset + socketAddressSize;
                }
            }
            catch (ArgumentNullException ex)
            {
                throw new InvalidOperationException("An argument provided to the method is null.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("An argument provided to the method is invalid.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while processing the buffer.", ex);
            }
        }

        /// <summary>
        /// 将ReadOnlyMemory转换为SocketAddress
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="socketAddress"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ByteToSocketAddress(ReadOnlyMemory<byte> buffer, int offset, out SocketAddress socketAddress)
        {
            if (buffer.Length < 8)
            {
                throw new ArgumentException("Buffer length is insufficient. It must be at least 8 bytes.", nameof(buffer));
            }

            try
            {
                fixed (byte* pBuffer = buffer.Span)
                {
                    var pOffsetBuffer = pBuffer + offset;
                    var addressFamily = (AddressFamily)Marshal.ReadInt32((IntPtr)pOffsetBuffer);
                    var socketAddressSize = Marshal.ReadInt32((IntPtr)(pOffsetBuffer + 4));

                    if (buffer.Length < offset + 8 + socketAddressSize)
                    {
                        throw new ArgumentException("Buffer length is insufficient for the given SocketAddress size.", nameof(buffer));
                    }

                    socketAddress = new SocketAddress(addressFamily, socketAddressSize);
                    
                    for (var i = 0; i < socketAddressSize - 2; i++)
                    {
                        socketAddress[i + 2] = *(pOffsetBuffer + 8 + i);
                    }

                    return 8 + offset + socketAddressSize;
                }
            }
            catch (ArgumentNullException ex)
            {
                throw new InvalidOperationException("An argument provided to the method is null.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("An argument provided to the method is invalid.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while processing the buffer.", ex);
            }
        }

        /// <summary>
        /// 根据SocketAddress获得IPEndPoint
        /// </summary>
        /// <param name="socketAddress"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static unsafe IPEndPoint GetIPEndPoint(this SocketAddress socketAddress)
        {
            switch (socketAddress.Family)
            {
                case AddressFamily.InterNetwork:
                {
                    var ipBytes = new byte[4];
                    for (var i = 0; i < 4; i++)
                    {
                        ipBytes[i] = socketAddress[4 + i];
                    }
                    var port = (socketAddress[2] << 8) + socketAddress[3];
                    var ip = new IPAddress(ipBytes);
                    return new IPEndPoint(ip, port);
                }
                case AddressFamily.InterNetworkV6:
                {
                    var ipBytes = new byte[16];
                    Span<byte> socketAddressSpan = stackalloc byte[28];
                    
                    for (var i = 0; i < 28; i++)
                    {
                        socketAddressSpan[i] = socketAddress[i];
                    }
                    
                    fixed (byte* pSocketAddress = socketAddressSpan)
                    {
                        for (var i = 0; i < 16; i++)
                        {
                            ipBytes[i] = *(pSocketAddress + 8 + i);
                        }
                        
                        var port = (*(pSocketAddress + 2) << 8) + *(pSocketAddress + 3);
                        var scopeId = Marshal.ReadInt64((IntPtr)(pSocketAddress + 24));
                        var ip = new IPAddress(ipBytes, scopeId);
                        return new IPEndPoint(ip, port);
                    }
                }
                default:
                {
                    throw new NotSupportedException("Address family not supported.");
                }
            }
        }
#endif
        /// <summary>
        /// 获取本机所有网络适配器的IP地址。
        /// </summary>
        /// <returns>IP地址数组。</returns>
        public static string[] GetAddressIPs()
        {
            var list = new List<string>();
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 仅考虑以太网类型的网络适配器
                if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                {
                    continue;
                }
    
                foreach (var add in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    list.Add(add.Address.ToString());
                }
            }
    
            return list.ToArray();
        }

        /// <summary>
        /// 将主机名和端口号转换为 <see cref="IPEndPoint"/> 实例。
        /// </summary>
        /// <param name="host">主机名。</param>
        /// <param name="port">端口号。</param>
        /// <returns><see cref="IPEndPoint"/> 实例。</returns>
        public static IPEndPoint ToIPEndPoint(string host, int port)
        {
            return new IPEndPoint(IPAddress.Parse(host), port);
        }

        /// <summary>
        /// 将地址字符串转换为 <see cref="IPEndPoint"/> 实例。
        /// </summary>
        /// <param name="address">地址字符串，格式为 "主机名:端口号"。</param>
        /// <returns><see cref="IPEndPoint"/> 实例。</returns>
        public static IPEndPoint ToIPEndPoint(string address)
        {
            var index = address.LastIndexOf(':');
            var host = address.Substring(0, index);
            var p = address.Substring(index + 1);
            var port = int.Parse(p);
            return ToIPEndPoint(host, port);
        }

        /// <summary>
        /// 将 <see cref="IPEndPoint"/> 实例转换为字符串表示形式。
        /// </summary>
        /// <param name="self"><see cref="IPEndPoint"/> 实例。</param>
        /// <returns>表示 <see cref="IPEndPoint"/> 的字符串。</returns>
        public static string IPEndPointToStr(this IPEndPoint self)
        {
            return $"{self.Address}:{self.Port}";
        }

        /// <summary>
        /// 针对 Windows 平台设置UDP连接重置选项。
        /// </summary>
        /// <param name="socket">要设置选项的 <see cref="Socket"/> 实例。</param>
        public static void SetSioUdpConnReset(this Socket socket)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
    
            /*
            目前这个问题只有Windows下才会出现。
            服务器端在发送数据时捕获到了一个异常，
            这个异常导致原因应该是远程客户端的UDP监听已停止导致数据发送出错。
            按理说UDP是无连接的，报这个异常是不合理的
            这个异常让整UDP的服务监听也停止了。
            这样就因为一个客户端的数据发送无法到达而导致了服务挂了，所有客户端都无法与服务器通信了
            想详细了解看下https://blog.csdn.net/sunzhen6251/article/details/124168805*/
            const uint IOC_IN = 0x80000000;
            const uint IOC_VENDOR = 0x18000000;
            const int SIO_UDP_CONNRESET = unchecked((int) (IOC_IN | IOC_VENDOR | 12));
    
            socket.IOControl(SIO_UDP_CONNRESET, new[] {Convert.ToByte(false)}, null);
        }
        
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
#endif