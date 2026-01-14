using System;
using System.Collections;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;
using UnityEngine.UI;

public class SphereEvent : MonoBehaviour
{
    private Scene _scene;
    private Session _session;
    
    public Button Connect;
    public Button Subscribe;
    public Button Publish;
    public Button RevokeRemoteSubscriber;
    public Button Unsubscribe;
    
    void Start()
    {
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
        await Fantasy.Platform.Unity.Entry.Initialize();
        _scene = await Scene.Create(SceneRuntimeMode.MainThread);
        Connect.onClick.RemoveAllListeners();
        Connect.onClick.AddListener(OnConnectClick);
        Subscribe.onClick.RemoveAllListeners();
        Subscribe.onClick.AddListener(() =>
        {
            OnSubscribeClick().Coroutine();
        });
        Publish.onClick.RemoveAllListeners();
        Publish.onClick.AddListener(() =>
        {
            OnPublishClick().Coroutine();
        });
        RevokeRemoteSubscriber.onClick.RemoveAllListeners();
        RevokeRemoteSubscriber.onClick.AddListener(() =>
        {
            OnRevokeRemoteSubscriberClick().Coroutine();
        });
        Unsubscribe.onClick.RemoveAllListeners();
        Unsubscribe.onClick.AddListener(() =>
        {
            OnUnsubscribeClick().Coroutine();
        });
        RevokeRemoteSubscriber.interactable = false;
        Unsubscribe.interactable = false;
    }

    private void OnConnectClick()
    {
        Connect.interactable = false;
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            OnConnectComplete,
            OnConnectFail,
            OnConnectDisconnect,
            false, 5000);
    }

    
    private async FTask OnSubscribeClick()
    {
        var response = await _session.C2G_SubscribeSphereEventRequest();
        if (response.ErrorCode == 0)
        {
            Subscribe.interactable = false;
            RevokeRemoteSubscriber.interactable = true;
            Unsubscribe.interactable = true;
        }
    }

    private async FTask OnPublishClick()
    {
        Publish.interactable = false;
        
        try
        {
            await _session.C2G_PublishSphereEventRequest();
        }
        finally
        {
            Publish.interactable = true;
        }
    }

    private async FTask OnRevokeRemoteSubscriberClick()
    {
        RevokeRemoteSubscriber.interactable = false;
        var response = await _session.C2G_UnsubscribeSphereEventRequest();
        if (response.ErrorCode == 0)
        {
            Subscribe.interactable = true;
            Unsubscribe.interactable = false;
        }
    }

    private async FTask OnUnsubscribeClick()
    {
        Unsubscribe.interactable = false;
        var response = await _session.C2G_MapUnsubscribeSphereEventRequest();
        if (response.ErrorCode == 0)
        {
            Subscribe.interactable = true;
            RevokeRemoteSubscriber.interactable = false;
        }
    }
    
    private void OnConnectComplete()
    {
        Log.Debug("连接成功");
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
        Connect.interactable = false;
    }

    private void OnConnectFail()
    {
        Log.Debug("连接失败");
        Connect.interactable = true;
    }

    private void OnConnectDisconnect()
    {
        Log.Debug("连接断开");
        Connect.interactable = true;
    }
}
