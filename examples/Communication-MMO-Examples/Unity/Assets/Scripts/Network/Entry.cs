using System;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Core.Network;
using Fantasy.Helper;
using Fantasy.Model;
using UnityEngine;

using BestGame;

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

    public long key;
    
    void Start()
    {
        // 框架初始化
        Realm = Fantasy.Entry.Initialize(); 
        Gate  = Scene.Create();

        // 把当前工程的程序集装载到框架中、这样框架才会正常的操作
        // 装载后例如网络协议等一些框架提供的功能就可以使用了
        AssemblyManager.Load(AssemblyName.AssemblyCSharp, GetType().Assembly);

        // 根据cdn json文件取得realm服，练习直接写在这里
        string json = "[{'ZoneId':1,'RealmAdress':'127.0.0.1:20001'},{'ZoneId':1,'RealmAdress':'127.0.0.1:20001'}]";
        var realmAdress = JsonHelper.Deserialize<List<RealmInfo>>(json);
        Log.Info(realmAdress[0].RealmAdress);

        // networkProtocolType:网络协议类型
            // 这里要使用与后端SceneConfig配置文件中配置的NetworkProtocolType类型一样才能建立连接
        Realm.CreateSession(realmAdress[0].RealmAdress, NetworkProtocolType.KCP, OnRealmConnected, OnConnectFail, null);
    }

    private async FTask RealmTest()
    {
        // 获取区服表
        R2C_GetZoneList zoneList = (R2C_GetZoneList) await Realm.Session.Call(new C2R_GetZoneList(){});
        Log.Info(zoneList.ToJson());
        
        // 请求realm验证
        R2C_RegisterResponse register = (R2C_RegisterResponse) await Realm.Session.Call(new C2R_RegisterRequest(){
            AuthName = "test",Pw = "123321",Pw2 = "123321", ZoneId = 1, Version = ConstValue.Version
        });

        // 登录realm账号
        R2C_LoginResponse loginRealm = (R2C_LoginResponse) await Realm.Session.Call(new C2R_LoginRequest(){
            AuthName = "test",Pw = "123321", Version = ConstValue.Version
        });
        Log.Info(register.ErrorCode.ToJson());
        Log.Info($"addres:{loginRealm.GateAddress}-port:{loginRealm.GatePort}-key:{loginRealm.Key}");

        // 建立与网关的连接，只有与网关的连接才需要挂心跳
        var gateAdress = loginRealm.GateAddress+":"+loginRealm.GatePort;
        key = loginRealm.Key;
        Gate.CreateSession(gateAdress, NetworkProtocolType.KCP, OnGateConnected, OnConnectFail, null);
    }

    private async FTask GateTest()
    {
        // 登录网关
            // 登录网关后创建角色，或者加载角色列表，选择角色进入游戏地图
        var loginGate = (G2C_LoginGateResponse) await Gate.Session.Call(new C2G_LoginGateRequest(){
            AuthName = "test", Key = key,
        });

        // 创建角色请求
        // var create = (G2C_RoleCreateResponse) await Gate.Session.Call(new C2G_RoleCreateRequest(){
        //     NickName = "roubin2",Sex = 1,Class = "Magic", UnitConfigId = 12012
        // });
        // Log.Info(create.RoleInfo.ToJson());

        // 获取角色列表
        var getRoles = (G2C_RoleListResponse) await Gate.Session.Call(new C2G_RoleListRequest(){});
        Log.Info(getRoles.Items.ToJson());

        // 进入地图请求(测试这个要先获取角色列表，不然网关账号缓存中没有角色)
        var enter = (G2C_EnterMapResponse) await Gate.Session.Call(new C2G_EnterMapRequest(){
            MapNum = 12314, RoleId = 457749024540262403
        });

        Log.Info(loginGate.ErrorCode.ToJson());
        

        // Map测试
        // MapTest();
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
