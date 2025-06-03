using System.Collections;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;
using UnityEngine.UI;

public class Roaming : MonoBehaviour
{
    public Button Connect;
    public Button CallRoamingMessage;
    public Button SendRoamingMessage;
    public Button RoamingTransferMessage;
    public Button SendInnerRoamingMessage;

    private Scene _scene;
    private Session _session;
    
    void Start()
    {
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
        
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
    
    private void OnConnectComplete()
    {
        Connect.interactable = false;
        CallRoamingMessage.interactable = true;
        SendRoamingMessage.interactable = true;
        RoamingTransferMessage.interactable = true;
        SendInnerRoamingMessage.interactable = true;
    }

    private void OnConnectFail()
    {
        Connect.interactable = true;
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
    }

    private void OnConnectDisconnect()
    {
        Connect.interactable = true;
        CallRoamingMessage.interactable = false;
        SendRoamingMessage.interactable = false;
        RoamingTransferMessage.interactable = false;
        SendInnerRoamingMessage.interactable = false;
    }
}
