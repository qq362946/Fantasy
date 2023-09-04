using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Fantasy.Helper
{
    /// <summary>
    /// 提供网络操作相关的帮助方法。
    /// </summary>
    public static class NetworkHelper
    {
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
        public static void SetSioUdpConnReset(Socket socket)
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
    }
}