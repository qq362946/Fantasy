using System.Collections;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public class RouteMessage : MonoBehaviour
{
    public Button Button1;
    public Button Button2;
    public Button Button3;
    public Button Button4;
    
    private Scene _scene;
    private Session _session;
    
    private void Start()
    {
        // 详细操作步骤，都在服务器的G2Chat_CreateRouteRequestHandler.cs文件里有详细说明。
        Button2.interactable = false;
        Button3.interactable = false;
        Button4.interactable = false;
        Button1.onClick.RemoveAllListeners();
        Button1.onClick.AddListener(() =>
        {
            Connect().Coroutine();
        });
        Button2.onClick.RemoveAllListeners();
        Button2.onClick.AddListener(() =>
        {
            RequestRouteToGate().Coroutine();
        });
        Button3.onClick.RemoveAllListeners();
        Button3.onClick.AddListener(SendRouteMessage);
        Button4.onClick.RemoveAllListeners();
        Button4.onClick.AddListener(() =>
        {
            CallRouteMessage().Coroutine();
        });
    }

    private async FTask Connect()
    {
        Button1.interactable = false;
        _scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            () =>
            {
                Button2.interactable = true;
                Log.Debug("连接到服务器完成");
                _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
            },
            () =>
            {
                Button1.interactable = true;
                Log.Error("无法连接到目标服务器");
            },
            () =>
            {
                Button1.interactable = true;
                Log.Debug("与服务器断开了连接");
            },
            false, 5000);
    }

    private async FTask RequestRouteToGate()
    {
        Button2.interactable = false;
        var response = (G2C_CreateChatRouteResponse)await _session.Call(new C2G_CreateChatRouteRequest());
        
        if (response.ErrorCode != 0)
        {
            Button2.interactable = true;
            Log.Error($"Failed to request route to gate ErrorCode = {response.ErrorCode}");
            return;
        }

        Button3.interactable = true;
        Button4.interactable = true;
        Log.Debug($"Route连接已经建立完成");
    }

    private void SendRouteMessage()
    {
        Button3.interactable = false;
        _session.Send(new C2Chat_TestMessage()
        {
            Tag = "Hello RouteMessage",
        });
        Button3.interactable = true;
    }

    private async FTask CallRouteMessage()
    {
        Button4.interactable = false;
        var response = (Chat2C_TestMessageResponse)await _session.Call(new C2Chat_TestMessageRequest()
        {
            Tag = "Hello RouteRPCMessage",
        });
        if (response.ErrorCode != 0)
        {
            Button4.interactable = true;
            Log.Error($"Failed to call route message ErrorCode = {response.ErrorCode}");
        }
        Button4.interactable = true;
        Log.Debug($"收到Chat发送来的消息 Tag = {response.Tag}");
    }
}
