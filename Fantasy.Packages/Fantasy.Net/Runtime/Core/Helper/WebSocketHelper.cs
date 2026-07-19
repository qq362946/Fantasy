using System;
using System.Net;
using System.Net.Sockets;

namespace Fantasy.Helper
{
    /// <summary>
    /// WebSocket帮助类
    /// </summary>
    public static partial class WebSocketHelper
    {
        /// <summary>
        /// 根据字符串获取WebSocket的连接地址
        /// </summary>
        /// <param name="address">目标服务器地址格式为:127.0.0.1:2000 或 [::1]:2000</param>
        /// <param name="isHttps">目标服务器是否为加密连接也就是https</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static string GetWebSocketAddress(string address, bool isHttps)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new FormatException("Address cannot be null or empty");
            }

            var portSeparator = address.LastIndexOf(':');
            if (portSeparator <= 0)
            {
                throw new FormatException("Invalid format: missing port separator ':'");
            }

            if (!ushort.TryParse(address.AsSpan(portSeparator + 1), out _))
            {
                throw new FormatException("Invalid port number");
            }

            var protocol = isHttps ? "wss://" : "ws://";

            // 已规范化的IPv6、IPv4和域名都只需要一次字符串分配。
            if (address[0] == '[')
            {
                if (address[portSeparator - 1] != ']')
                {
                    throw new FormatException("Invalid IPv6 format: missing closing bracket");
                }

                return string.Concat(protocol, address);
            }

            var host = address.AsSpan(0, portSeparator);
            if (!IPAddress.TryParse(host, out var ipAddress) || ipAddress.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return string.Concat(protocol, address);
            }

            // 不带方括号的IPv6需要规范化为RFC 3986格式。
            return string.Create(
                protocol.Length + address.Length + 2,
                (protocol, address, portSeparator),
                static (result, state) =>
                {
                    state.protocol.AsSpan().CopyTo(result);
                    var offset = state.protocol.Length;
                    result[offset++] = '[';
                    state.address.AsSpan(0, state.portSeparator).CopyTo(result[offset..]);
                    offset += state.portSeparator;
                    result[offset++] = ']';
                    state.address.AsSpan(state.portSeparator).CopyTo(result[offset..]);
                });
        }
    }
}
