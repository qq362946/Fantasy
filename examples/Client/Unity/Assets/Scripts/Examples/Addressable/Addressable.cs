using System.Collections;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public class Addressable : MonoBehaviour
{
    public Text Text;
    public Button ConnectAddressable;
    public Button SendAddressableMessage;
    public Button SendAddressableRPC;
    private Scene _scene;
    private Session _session;
    private void Start()
    {
        SendAddressableMessage.interactable = false;
        SendAddressableRPC.interactable = false;
        
        ConnectAddressable.onClick.RemoveAllListeners();
        ConnectAddressable.onClick.AddListener(() =>
        {
            OnConnectAddressableClick().Coroutine();
        });
        
        SendAddressableMessage.onClick.RemoveAllListeners();
        SendAddressableMessage.onClick.AddListener(OnSendAddressableMessageClick);
        
        SendAddressableRPC.onClick.RemoveAllListeners();
        SendAddressableRPC.onClick.AddListener(() =>
        {
            OnSendAddressableRPCClick().Coroutine();
        });
    }

    #region Connect

    private async FTask OnConnectAddressableClick()
    {
        ConnectAddressable.interactable = false;
        // 1、初始化Fantasy
        _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        // 2、连接到Gate服务器
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            OnConnectComplete,
            OnConnectFail,
            OnConnectDisconnect,
            false, 5000);
        // 3、发送C2G_CreateAddressableRequest协议给Gate，进行创建Addressable.
        var response = (G2C_CreateAddressableResponse)await _session.Call(new C2G_CreateAddressableRequest());
        if (response.ErrorCode != 0)
        {
            Log.Debug("创建Addressable失败！");
            ConnectAddressable.interactable = true;
            SendAddressableMessage.interactable = false;
            SendAddressableRPC.interactable = false;
            return;
        }
        Log.Debug("创建Addressable成功！");
        SendAddressableMessage.interactable = true;
        SendAddressableRPC.interactable = true;
    }
    
    private void OnConnectComplete()
    {
        Text.text = "连接成功";
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
        ConnectAddressable.interactable = false;
    }

    private void OnConnectFail()
    {
        Text.text = "连接失败";
        ConnectAddressable.interactable = true;
        SendAddressableMessage.interactable = false;
        SendAddressableRPC.interactable = false;
    }

    private void OnConnectDisconnect()
    {
        Text.text = "连接断开";
        ConnectAddressable.interactable = true;
        SendAddressableMessage.interactable = false;
        SendAddressableRPC.interactable = false;
    }

    #endregion

    #region SendAddressableMessage

    private void OnSendAddressableMessageClick()
    {
        SendAddressableMessage.interactable = false;
        // 发送一个消息给Gate服务器，Gate服务器会自动转发到Map服务器上
        // 流程: Client -> Gate -> Map
        _session.Send(new C2M_TestMessage()
        {
            Tag = "Hello C2M_TestMessage"
        });
        SendAddressableMessage.interactable = true;
    }

    #endregion

    #region SendAddressableRPC

    private async FTask OnSendAddressableRPCClick()
    {
        SendAddressableRPC.interactable = false;
        // 发送一个RPC消息
        // C2M_TestRequest:Map服务器接收的协议 流程:Client -> Gate -> Map
        // M2C_TestResponse:客户端接收到服务器发送的返回消息 流程:Map -> Gate -> Client 
        var response = (M2C_TestResponse)await _session.Call(new C2M_TestRequest()
        {
            Tag = "Hello C2M_TestRequest"
        });
        Text.text = $"收到M2C_TestResponse Tag = {response.Tag}";
        SendAddressableRPC.interactable = true;
    }

    #endregion
}
