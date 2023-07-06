// using Fantasy.Core;
// using Fantasy.Core.Network;
//
// namespace Fantasy.Model
// {
//     public sealed class LoginUIAwakeSystem : AwakeSystem<LoginUI>
//     {
//         protected override void Awake(LoginUI self)
//         {
//             self.LoginButton.onClick.RemoveAllListeners();
//             self.LoginButton.onClick.AddListener(self.OnLoginButtonClick);
//         }
//     }
//
//     public static class LoginHelper
//     {
//         public static void OnLoginButtonClick(this LoginUI self)
//         {
//             Log.Debug("点击了LoginButton");
//             // 创建一个网络连接
//             // 后面可以Scene.Session来发送消息
//             self.Scene.CreateSession("127.0.0.1:20000", NetworkProtocolType.KCP, () =>
//             {
//                 Log.Error("无法连接到服务器");
//             });
//             // 发送给服务器登录的消息
//             self.Login().Coroutine();
//         }
//
//         private static async FTask Login(this LoginUI self)
//         {
//             // 发送一个RPC消息、协议在Demo跟目录Config/ProtoBuf里定义的
//             var response = (H_G2C_LoginResponse)await self.Scene.Session.Call(new H_C2G_LoginRequest()
//             {
//                 UserName = "Fantasy", Password = "666"
//             });
//
//             if (response.ErrorCode != 0)
//             {
//                 Log.Error($"登录失败 错误码:{response.ErrorCode}");
//                 return;
//             }
//             
//             Log.Debug($"接收到服务器返回的消息是:{response.Text}");
//             
//             // 登录成功、跳转到游戏大厅界面
//             // 首先要关闭当前UI
//             self.Dispose();
//             // 添加UI到UI管理器中
//             UIComponent.Instance.AddComponent<LobbyUI>();
//         }
//     }
// }