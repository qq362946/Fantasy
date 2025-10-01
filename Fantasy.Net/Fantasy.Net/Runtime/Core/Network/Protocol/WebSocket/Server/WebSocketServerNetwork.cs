#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Fantasy.Async;
using Fantasy.Network.Interface;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Network.WebSocket;

public class WebSocketServerNetwork : ANetwork
{
    private Random _random;
    private HttpListener _httpListener;
    private readonly Dictionary<uint, INetworkChannel> _connectionChannel = new Dictionary<uint, INetworkChannel>();

    public void Initialize(NetworkTarget networkTarget, int port)
    {
        base.Initialize(NetworkType.Server, NetworkProtocolType.WebSocket, networkTarget);
        
        try
        {
            _random = new Random();
            _httpListener = new HttpListener();
            StartAcceptAsync(port).Coroutine();
        }
        catch (HttpListenerException e)
        {
            if (e.ErrorCode == 5)
            {
                throw new Exception($"如果看下如下错误请尝试下面的办法:" +
                                    $"1.用管理员运行CMD 输入命令: netsh http add urlacl url=http://+:8080/ user=Everyone。" +
                                    $"2.用管理员身份运行编辑器或者程序。", e);
            }

            Log.Error(e);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_httpListener != null)
        {
            _httpListener.Close();
            _httpListener = null;
        }
        
        foreach (var channel in _connectionChannel.Values.ToArray())
        {
            channel.Dispose();
        }
        
        _connectionChannel.Clear();
        base.Dispose();
    }

    private static bool IsValidWebSocketRequest(HttpListenerRequest request)
    {
        // 检查必需的WebSocket握手头部
        var connectionHeader = request.Headers["Connection"];
        var upgradeHeader = request.Headers["Upgrade"];
        
        if (string.IsNullOrEmpty(connectionHeader) || string.IsNullOrEmpty(upgradeHeader))
        {
            return false;
        }
        
        // Connection头部应该包含"Upgrade" (不区分大小写)
        if (!connectionHeader.ToLower().Contains("upgrade"))
        {
            return false;
        }
        
        // Upgrade头部应该是"websocket" (不区分大小写)  
        if (!upgradeHeader.ToLower().Contains("websocket"))
        {
            return false;
        }
        
        // 检查WebSocket版本 (可选但推荐)
        var versionHeader = request.Headers["Sec-WebSocket-Version"];
        if (!string.IsNullOrEmpty(versionHeader) && versionHeader != "13")
        {
            return false;
        }
        
        return true;
    }

    private async FTask StartAcceptAsync(int port)
    {
        var listenUrl = $"http://+:{port}/";
        _httpListener.Prefixes.Add(listenUrl);
        _httpListener.Start();
        Log.Info($"SceneConfigId = {Scene.SceneConfigId} WebSocketServer Listen {listenUrl}");
        while (!IsDisposed)
        {
            try
            {
                var httpListenerContext = await _httpListener.GetContextAsync();
                // 验证WebSocket握手请求头部
                if (!IsValidWebSocketRequest(httpListenerContext.Request))
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.StatusDescription = "Bad Request - Invalid WebSocket headers";
                    httpListenerContext.Response.Close();
                    continue;
                }
                var webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
                var channelId = 0xC0000000 | (uint) _random.Next();
                while (_connectionChannel.ContainsKey(channelId))
                {
                    channelId = 0xC0000000 | (uint) _random.Next();
                }

                var webSocketServerNetworkChannel = new WebSocketServerNetworkChannel(this, channelId, webSocketContext, httpListenerContext.Request.RemoteEndPoint);
                _connectionChannel.Add(channelId, webSocketServerNetworkChannel);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
    
    public override void RemoveChannel(uint channelId)
    {
        if (IsDisposed || !_connectionChannel.Remove(channelId, out var channel))
        {
            return;
        }
        
        if (channel.IsDisposed)
        {
            return;
        }

        channel.Dispose();
    }
}
#endif
