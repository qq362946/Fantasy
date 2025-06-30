using System.Collections;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using UnityEngine;
using UnityEngine.UI;

public sealed class Map2C_PushMessageToClientHandler : Message<Map2C_PushMessageToClient>
{
    protected override async FTask Run(Session session, Map2C_PushMessageToClient message)
    {
        Log.Debug($"Map2C_PushMessageToClient:{message.Tag}");
        await FTask.CompletedTask;
    }
}

public class Roaming : MonoBehaviour
{
    public Button Connect;
    public Button CallRoamingMessage;
    public Button SendRoamingMessage;
    public Button RoamingTransferMessage;
    public Button SendInnerRoamingMessage;
    public Button PushRoamingMessageToClient;
    public Button GateSendRouteToRoaming;
    public Button GateSendRoamingToRoaming;

    private Scene _scene;
    private Session _session;
    
    void Start()
    {
        StartAsync().Coroutine();
    }
    
    private void OnDestroy()
    {
        // 当Unity关闭或当前脚本销毁的时候，销毁这个Scene。
        // 这样网络和Fantasy的相关功能都会销毁掉了。
        // 这里只是展示一下如何销毁这个Scene的地方。
        // 但这里销毁的时机明显是不对的，应该放到一个全局的地方。
        _scene?.Dispose();
    }

    private async FTask StartAsync()
    {
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
        GateSendRouteToRoaming.interactable = false;
        GateSendRoamingToRoaming.interactable = false;
        
        Connect.onClick.RemoveAllListeners();
        Connect.onClick.AddListener(() =>
        {
            OnConnectClick().Coroutine();
        });
        
        SendRoamingMessage.onClick.RemoveAllListeners();
        SendRoamingMessage.onClick.AddListener(OnSendRoamingMessageClick);
        CallRoamingMessage.onClick.RemoveAllListeners();
        CallRoamingMessage.onClick.AddListener(() => OnCallRoamingMessageClick().Coroutine());
        RoamingTransferMessage.onClick.RemoveAllListeners();
        RoamingTransferMessage.onClick.AddListener(() => OnRoamingTransferMessageClick().Coroutine());
        SendInnerRoamingMessage.onClick.RemoveAllListeners();
        SendInnerRoamingMessage.onClick.AddListener(OnSendInnerRoamingMessageClick);
        PushRoamingMessageToClient.onClick.RemoveAllListeners();
        PushRoamingMessageToClient.onClick.AddListener(OnPushRoamingMessageToClientClick);
        GateSendRouteToRoaming.onClick.RemoveAllListeners();
        GateSendRouteToRoaming.onClick.AddListener(OnGateSendRouteToRoamingClick);
        GateSendRoamingToRoaming.onClick.RemoveAllListeners();
        GateSendRoamingToRoaming.onClick.AddListener(OnGateSendRoamingToRoamingClick);
        
        // 初始化框架
        await Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
        // 创建一个Scene，这个Scene代表一个客户端的场景，客户端的所有逻辑都可以写这里
        // 如果有自己的框架，也可以就单纯拿这个Scene做网络通讯也没问题。
        _scene = await Scene.Create(SceneRuntimeMode.MainThread);
    }

    private async FTask OnConnectClick()
    {
        // 连接到Gate服务器
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            OnConnectComplete,
            OnConnectFail,
            OnConnectDisconnect,
            false, 5000);
        var response = (G2C_ConnectRoamingResponse)await _session.Call(new C2G_ConnectRoamingRequest());
        if (response.ErrorCode != 0)
        {
            OnConnectFail();
            return;
        }
        Log.Debug("Roaming创建完成！");
        Connect.interactable = false;
    }

    private void OnSendRoamingMessageClick()
    {
        _session.Send(new C2Chat_TestRoamingMessage()
        {
            Tag = "Hi Roaming!"
        });
        _session.Send(new C2Map_TestRoamingMessage()
        {
            Tag = "Hi Roaming!"
        });
        // RandomHelper.BreakRank();
    }

    private async FTask OnCallRoamingMessageClick()
    {
        var response = (Chat2C_TestRPCRoamingResponse)await _session.Call(new C2Chat_TestRPCRoamingRequest()
        {
            Tag = "Hi RPCRoaming!"
        });
        if (response.ErrorCode != 0)
        {
            Log.Error($"OnCallRoamingMessageClick ErrorCode = {response.ErrorCode}");
        }
    }

    private async FTask OnRoamingTransferMessageClick()
    {
        var response = (Map2C_TestTransferResponse)await _session.Call(new C2Map_TestTransferRequest());
        if (response.ErrorCode != 0)
        {
            Log.Error($"OnRoamingTransferMessageClick ErrorCode = {response.ErrorCode}");
        }
        Log.Debug("传送漫游到Map完成！");
    }

    private void OnSendInnerRoamingMessageClick()
    {
        _session.Send(new C2Chat_TestSendMapMessage()
        {
            Tag = "Hi SendInnerRoamingMessage"
        });
    }

    private void OnPushRoamingMessageToClientClick()
    {
        _session.Send(new C2Map_PushMessageToClient()
        {
            Tag = "Push Message"
        });
    }

    private void OnGateSendRouteToRoamingClick()
    {
        _session.Send(new C2G_TestRouteToRoaming()
        {
            Tag = "Hi Roaming"
        });
    }
    
    private void OnGateSendRoamingToRoamingClick()
    {
        _session.Send(new C2G_TestRoamingToRoaming()
        {
            Tag = "Hi Roaming"
        });
    }
    
    private void OnConnectComplete()
    {
        Connect.interactable = false;
        CallRoamingMessage.interactable = true;
        SendRoamingMessage.interactable = true;
        RoamingTransferMessage.interactable = true;
        SendInnerRoamingMessage.interactable = true;
        GateSendRouteToRoaming.interactable = true;
        GateSendRoamingToRoaming.interactable = true;
    }

    private void OnConnectFail()
    {
        Connect.interactable = true;
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
        GateSendRouteToRoaming.interactable = false;
        GateSendRoamingToRoaming.interactable = false;
    }

    private void OnConnectDisconnect()
    {
        Connect.interactable = true;
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
        GateSendRouteToRoaming.interactable = false;
        GateSendRoamingToRoaming.interactable = false;
    }
}
