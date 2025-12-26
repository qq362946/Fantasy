using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Serialize;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Examples.Game_Examples
{
    public sealed class LoginGame : MonoBehaviour
    {
        [FormerlySerializedAs("Username")] 
        public InputField username;
        [FormerlySerializedAs("LoginButton")] 
        public Button loginButton;
        [FormerlySerializedAs("Players")] 
        public GameObject players;
        [FormerlySerializedAs("PlayerGameObject")] 
        public GameObject playerGameObject;
        
        public void Start()
        {
            Application.targetFrameRate = 30;
            
            username.text = "666";
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                OnLoginButtonClicked().Coroutine();
            });
        }

        private async FTask OnLoginButtonClicked()
        {
            if (string.IsNullOrEmpty(username.text))
            {
                Log.Error("Username is empty");
                return;
            }
            
            var response = await Fantasy.Runtime.Session.C2G_LoginGameRequest(username.text);
            if (response.ErrorCode != 0)
            {
                Log.Error($"登陆时发生错误 {response.ErrorCode}");
                return;
            }
            this.gameObject.SetActive(false);
        }

        public void OnConnectComplete()
        {
            Log.Debug("OnConnectComplete");
            var sceneFlagComponent = Fantasy.Runtime.Session.AddComponent<SceneFlagComponent>();
            sceneFlagComponent.Players = players;
            sceneFlagComponent.PlayerGameObject = playerGameObject;
        }
        
        public void OnConnectFailed()
        {
            Log.Debug("OnConnectFailed");
        }
        
        public void OnConnectDisconnect()
        {
            Log.Debug("OnConnectDisconnect");
        }
    }
}