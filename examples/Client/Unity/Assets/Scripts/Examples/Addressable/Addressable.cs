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
        await FTask.CompletedTask;
    }

    #endregion
}
