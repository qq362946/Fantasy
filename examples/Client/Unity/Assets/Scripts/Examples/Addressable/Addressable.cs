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
        _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            OnConnectComplete,
            OnConnectFail,
            OnConnectDisconnect,
            false, 5000);
    }
    
    private void OnConnectComplete()
    {
        Text.text = "连接成功";
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
        ConnectAddressable.interactable = false;
        SendAddressableMessage.interactable = true;
        SendAddressableMessage.interactable = true;
    }

    private void OnConnectFail()
    {
        Text.text = "连接失败";
        ConnectAddressable.interactable = true;
        SendAddressableMessage.interactable = false;
        SendAddressableMessage.interactable = false;
    }

    private void OnConnectDisconnect()
    {
        Text.text = "连接断开";
        ConnectAddressable.interactable = true;
        SendAddressableMessage.interactable = false;
        SendAddressableMessage.interactable = false;
    }

    #endregion

    #region SendAddressableMessage

    private void OnSendAddressableMessageClick()
    {
        SendAddressableMessage.interactable = false;
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
