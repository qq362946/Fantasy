using System;
using System.Runtime.CompilerServices;

namespace Fantasy
{
    public static class WebSocketHelper
    {
        /// <summary>
        /// 根据字符串获取WebSocket的连接地址
        /// </summary>
        /// <param name="address">目标服务器地址格式为:127.0.0.1:2000</param>
        /// <param name="isHttps">目标服务器是否为加密连接也就是https</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetWebSocketAddress(string address, bool isHttps)
        {
            var addressSplit = address.Split(':');
            if (addressSplit.Length != 2)
            {
                throw new FormatException("Invalid format");
            }
                
            var ipString = addressSplit[0];
            var portString = addressSplit[1];
                
            if (!int.TryParse(portString, out var port) || port < 0 || port > 65535)
            {
                throw new FormatException("Invalid port number");
            }

            return isHttps ? $"wss://{ipString}:{portString}" : $"ws://{ipString}:{portString}";
        }
    }
}