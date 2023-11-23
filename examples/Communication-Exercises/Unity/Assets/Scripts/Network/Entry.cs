using System;
using Fantasy;
using UnityEngine;
using BestGame;
using Object = UnityEngine.Object;

// 前端发送接口，方便在各unity的脚本中，调用建立session连接的脚本，向服务器发送请求与消息
// 也可以把在前端创建Scene实例引用存入单例，你可以在各脚本能访问到Scene，就能向服务器发消息
// 用接口更解耦一些，而且发送消息放在专门负责发送接收消息方法的组件上，好很多。
public interface ISend
{
    void MoveSend(Vector3 position ,Quaternion rotation);
}

public class Entry : MonoBehaviour,ISend
{
    public Scene Realm;
    public Scene Gate;
    public bool IsConnect; 
    
    void Start()
    {
        // 框架初始化
        Realm = Fantasy.Entry.Initialize(); 
        Gate  = Scene.Create();
        // 把当前工程的程序集装载到框架中、这样框架才会正常的操作
        // 装载后例如网络协议等一些框架提供的功能就可以使用了
        AssemblyManager.Load(AssemblyName.AssemblyCSharp, GetType().Assembly);
        // 连接服务器
        ConnectServer();
    }

    private async FTask RealmTest()
    {
        // 请求realm验证
        R2C_RegisterResponse register = (R2C_RegisterResponse) await Realm.Session.Call(new C2R_RegisterRequest(){
            UserName = "test",Password = ""
        });
        Log.Info(register.Message);

        // 登录realm账号
        R2C_LoginResponse loginRealm = (R2C_LoginResponse) await Realm.Session.Call(new C2R_LoginRequest(){
            UserName = "test",Password = ""
        });
        Log.Info(loginRealm.Message);
    }

    private async FTask GateTest()
    {
        // 登录网关
            // 登录网关后创建角色，或者加载角色列表，选择角色进入游戏地图
        G2C_LoginGateResponse loginGate = (G2C_LoginGateResponse) await Gate.Session.Call(new C2G_LoginGateRequest(){
            Message = "请求登录网关"
        });
        Log.Info(loginGate.Message);

        // 创建角色请求
        G2C_CreateCharacterResponse create = (G2C_CreateCharacterResponse) await Gate.Session.Call(new C2G_CreateCharacterRequest(){
            Message = "请求创建角色"
        });
        Log.Info(create.Message);

        // 进入地图请求
        G2C_EnterMapResponse enter = (G2C_EnterMapResponse) await Gate.Session.Call(new C2G_EnterMapRequest(){
            Message = "请求进入地图"
        });
        Log.Info(enter.Message);

        // Map测试
        MapTest();
    }

    private void MapTest()
    {
        GameObject player = new GameObject("Player");
        player.AddComponent<PlayerMoveSender>();
    }
    // 发送Move数据的接口方法
    public void MoveSend(Vector3 position ,Quaternion rotation)
    {
        var moveInfo = MessageInfoHelper.MoveInfo(position,rotation);
        Gate.Session.Send(new C2M_MoveMessage{
            MoveInfo = moveInfo
        });
    }

    private void ConnectServer()
    {
        // 创建网络连接
            // 外网访问的是SceneConfig配置文件中配置的Gate 20000端口,Realm 20001端口
        // networkProtocolType:网络协议类型
            // 这里要使用与后端SceneConfig配置文件中配置的NetworkProtocolType类型一样才能建立连接
        Realm.CreateSession("127.0.0.1:20001", NetworkProtocolType.KCP, OnRealmConnected, OnConnectFail, null);

        // 建立与网关的连接，只有与网关的连接才需要挂心跳
        Gate.CreateSession("127.0.0.1:20000", NetworkProtocolType.KCP, OnGateConnected, OnConnectFail, null);
    }

    private void OnRealmConnected()
    {
        Log.Debug("已连接到Realm服务器");
        RealmTest().Coroutine();
    }

    private void OnGateConnected()
    {
        IsConnect = true;
        // 挂载心跳组件，设置每隔3000毫秒发送一次心跳给服务器
        // 只需要给客户端保持连接的服务器挂心跳
        Gate.Session.AddComponent<SessionHeartbeatComponent>().Start(3000);
        Log.Debug("已连接到网关服务器");

        GateTest().Coroutine();
    }

    private void OnConnectFail()
    {
        IsConnect = false;
        Log.Error("无法连接到服务器");
    }
}

