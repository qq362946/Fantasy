using Fantasy;
using UnityEngine;
using UnityEngine.UI;

// 协议在Examples/Config/ProtoBuf/Outer/OuterMessage.proto定义
// 服务器接收的对应文件位置Examples/Server/Hotfix/Outer/NormalMessage/Gate
public class NormalMessage : MonoBehaviour
{
    public Text Text;
    public Button ConnectButton;
    public Button SendMessageButton;
    public Button SendRPCMessageButton;
    
    private Scene _scene;
    private Session _session;
    private void Start()
    {
        ConnectButton.onClick.RemoveAllListeners();
        ConnectButton.onClick.AddListener(() =>
        {
            OnConnectButtonClick().Coroutine();
        });
        
        SendMessageButton.onClick.RemoveAllListeners();
        SendMessageButton.onClick.AddListener(OnSendMessageButtonClick);
        
        SendRPCMessageButton.onClick.RemoveAllListeners();
        SendRPCMessageButton.onClick.AddListener(() =>
        {
            OnSendRPCMessageButtonClick().Coroutine();
        });
    }

    #region Connect

    private long timerId;
    private async FTask OnConnectButtonClick()
    {
        ConnectButton.interactable = false;
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
        ConnectButton.interactable = false;
        SendMessageButton.interactable = true;
        SendRPCMessageButton.interactable = true;
    }

    private void OnConnectFail()
    {
        Text.text = "连接失败";
        ConnectButton.interactable = true;
        SendMessageButton.interactable = false;
        SendRPCMessageButton.interactable = false;
    }

    private void OnConnectDisconnect()
    {
        Text.text = "连接断开";
        ConnectButton.interactable = true;
        SendMessageButton.interactable = false;
        SendRPCMessageButton.interactable = false;
    }

    #endregion

    #region SendMessage

    private void OnSendMessageButtonClick()
    {
        SendMessageButton.interactable = false;
        // 发送一个消息给服务器
        _session.Send(new C2G_TestMessage() { Tag = "Hello C2G_TestMessage" });
        SendMessageButton.interactable = true;
    }

    #endregion

    #region SendRPCMessage

    private async FTask OnSendRPCMessageButtonClick()
    {
        SendRPCMessageButton.interactable = false;
        // 发送一个RPC消息
        // C2G_TestRequest:服务器接收的协议
        // G2C_TestResponse:客户端接收到服务器发送的返回消息
        var response = (G2C_TestResponse)await _session.Call(new C2G_TestRequest()
        {
            Tag = "Hello C2G_TestRequest"
        });
        Text.text = $"收到G2C_TestResponse Tag = {response.Tag}";
        SendRPCMessageButton.interactable = true;
    }

    #endregion
    
    private void OnDestroy()
    {
        // 当Unity关闭或当前脚本销毁的时候，销毁这个Scene。
        // 这样网络和Fantasy的相关功能都会销毁掉了。
        // 这里只是展示一下如何销毁这个Scene的地方。
        // 但这里销毁的时机明显是不对的，应该放到一个全局的地方。
        _scene?.Dispose();
    }
}
