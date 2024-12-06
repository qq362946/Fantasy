using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;
using UnityEngine.UI;

public class SubScene : MonoBehaviour
{
     private Scene _scene;
     private Session _session;
     
     public Button ConnectButton;
     public Button CreateSubSceneButton;
     public Button SendMessageButton;
     public Button CreaateAddressabeButton;
     public Button SendAddressableButton;

     public void Start()
     {
          ConnectButton.interactable = false;
          CreateSubSceneButton.interactable = false;
          SendMessageButton.interactable = false;
          CreaateAddressabeButton.interactable = false;
          SendAddressableButton.interactable = false;
          StartAsync().Coroutine();
     }

     private async FTask StartAsync()
     {
          // 初始化框架
          Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
          // 如果有自己的框架，也可以就单纯拿这个Scene做网络通讯也没问题。
          _scene = await Scene.Create(SceneRuntimeType.MainThread);
          ConnectButton.onClick.RemoveAllListeners();
          ConnectButton.onClick.AddListener(Connect);
          CreateSubSceneButton.onClick.RemoveAllListeners();
          CreateSubSceneButton.onClick.AddListener(() =>
          {
               CreateSubScene().Coroutine();
          });
          SendMessageButton.onClick.RemoveAllListeners();
          SendMessageButton.onClick.AddListener(SendMessage);
          CreaateAddressabeButton.onClick.RemoveAllListeners();
          CreaateAddressabeButton.onClick.AddListener(() =>
          {
               CreateAddressable().Coroutine();
          });
          SendAddressableButton.onClick.RemoveAllListeners();
          SendAddressableButton.onClick.AddListener(SendAddressable);
          ConnectButton.interactable = true;
     }

     private void Connect()
     {
          ConnectButton.interactable = false;
          _session = _scene.Connect(
               "127.0.0.1:20000",
               NetworkProtocolType.KCP,
               () =>
               {
                    ConnectButton.interactable = false;
                    CreateSubSceneButton.interactable = true;
                    SendMessageButton.interactable = true;
                    CreaateAddressabeButton.interactable = true;
                    SendAddressableButton.interactable = true;
                    Log.Debug("连接到服务器完成");
                    _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
               },
               () =>
               {
                    ConnectButton.interactable = true;
                    CreateSubSceneButton.interactable = false;
                    SendMessageButton.interactable = false;
                    CreaateAddressabeButton.interactable = false;
                    SendAddressableButton.interactable = false;
                    Log.Error("无法连接到目标服务器");
               },
               () =>
               {
                    ConnectButton.interactable = false;
                    CreateSubSceneButton.interactable = false;
                    SendMessageButton.interactable = false;
                    CreaateAddressabeButton.interactable = false;
                    SendAddressableButton.interactable = false;
                    Log.Debug("与服务器断开了连接");
               },
               false, 5000);
     }

     private async FTask CreateSubScene()
     {
          var response = (G2C_CreateSubSceneResponse)await _session.Call(new C2G_CreateSubSceneRequest());
          if (response.ErrorCode != 0)
          {
               Log.Debug($"创建SubScene失败 ErrorCode: {response.ErrorCode}");
                return;
          }
          Log.Debug("创建子场景成功");
     }

     private void SendMessage()
     {
          _session.Send(new C2G_SendToSubSceneMessage());
     }

     private async FTask CreateAddressable()
     {
          var response = (G2C_CreateSubSceneAddressableResponse)await _session.Call(new C2G_CreateSubSceneAddressableRequest());
          if (response.ErrorCode != 0)
          {
               Log.Debug($"创建SubSceneAddressable失败 ErrorCode: {response.ErrorCode}");
               return;
          }

          Log.Debug("创建SubSceneAddressable成功");
     }

     private void SendAddressable()
     {
          _session.Send(new C2SubScene_TestMessage()
          {
               Tag = "hi subScene Addressable"
          });
     }
}