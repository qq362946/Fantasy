#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#if FANTASY_NET
using System.Net;
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

    public void Initialize(NetworkTarget networkTarget, IEnumerable<string> urls)
    {
        base.Initialize(NetworkType.Server, NetworkProtocolType.WebSocket, networkTarget);
        
        try
        {
            _random = new Random();
            _httpListener = new HttpListener();
            StartAcceptAsync(urls).Coroutine();
            Log.Info($"SceneConfigId = {Scene.SceneConfigId} WebSocketServer Listen {urls.FirstOrDefault()}");
        }
        catch (HttpListenerException e)
        {
            if (e.ErrorCode == 5)
            {
                throw new Exception($"CMD管理员中输入: netsh http add urlacl url=http://*:8080/ user=Everyone", e);
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

    private async FTask StartAcceptAsync(IEnumerable<string> urls)
    {
        foreach (var prefix in urls)
        {
            _httpListener.Prefixes.Add(prefix);
        }
        _httpListener.Start();
        
        while (!IsDisposed)
        {
            try
            {
                var httpListenerContext = await _httpListener.GetContextAsync();
                var webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
                var channelId = 0xC0000000 | (uint) _random.Next();

                while (_connectionChannel.ContainsKey(channelId))
                {
                    channelId = 0xC0000000 | (uint) _random.Next();
                }
            
                _connectionChannel.Add(channelId, new WebSocketServerNetworkChannel(this, channelId, webSocketContext, httpListenerContext.Request.RemoteEndPoint));
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
