using UnityEngine;
using System.Collections.Generic;
using Fantasy;

public partial class Sender : SingletonMono<Sender>
{
    public override bool DontDestroy => true;
    public Fantasy.Scene RealmScene;
    public Fantasy.Scene GateScene;

    private string json = "";
    public List<RealmInfo> zoneInfo;
    public string gateAdress = "";
    public RealmInfo defaultZone;
    public RealmInfo selectZone;

    public void GameNetStart(){
        RealmScene = Fantasy.Entry.Initialize();
        GateScene = Fantasy.Scene.Create();

        // 根据cdn json文件取得realm服，练习直接写在这里
        json = @"[
            {'RegionId':1,'ZoneId':1,'ZoneName':'骑士崛起','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':1,'ZoneId':2,'ZoneName':'幻想黎明','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':1,'ZoneId':3,'ZoneName':'魔法森林','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':1,'ZoneId':4,'ZoneName':'黎明海岸','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':1,'ZoneId':5,'ZoneName':'幻想大陆','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':2,'ZoneId':6,'ZoneName':'奇迹之城','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':2,'ZoneId':7,'ZoneName':'幻想之刃','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':2,'ZoneId':8,'ZoneName':'暗影之境','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':2,'ZoneId':9,'ZoneName':'圣光之地','RealmAdress':'127.0.0.1:20001'},
            {'RegionId':2,'ZoneId':10,'ZoneName':'龙之巢穴','RealmAdress':'127.0.0.1:20001'},
            ]";
            
        zoneInfo = JsonHelper.Deserialize<List<RealmInfo>>(json);
        selectZone = defaultZone = zoneInfo[0];
    }

    public async FTask<IResponse> Register(IRequest request)
    {
        return await Realm.Call(request);
    }

    public async FTask<IResponse> Login(IRequest request)
    {
        return await Realm.Call(request);
    }

    public void Send(IMessage message)
    {
        Gate.Send(message);
    }

    public void Send(IAddressableRouteMessage message)
    {
        Gate.Send(message);
    }

    public async FTask<IResponse> Call(IRequest request)
    {
        return await Gate.Call(request);
    }

    public async FTask<IAddressableRouteResponse> Call(IAddressableRouteRequest request)
    {
        return (IAddressableRouteResponse) await Gate.Call(request);
    }

    // 网关服务器是否连接
    public static bool IsConnect{
        get{
            Session session = gateRef;
            return session==null?false:true;
        }
    }

    public static EntityReference<Session> gateRef;
    public Session Gate{
        get{
            Session session = gateRef;
            if(session==null)
            {
                GateScene.CreateSession(gateAdress, NetworkProtocolType.KCP, 
                    OnGateConnected, OnConnectFail, null);
                return gateRef = GateScene.Session;
            }
            return session;
        }
    }
    public static EntityReference<Session> realmRef;
    public Session Realm{
        get{
            Session session = realmRef;
            if(session==null)
            {
                RealmScene.CreateSession(selectZone.RealmAdress, NetworkProtocolType.KCP,
                    OnRealmConnected, OnConnectFail, null);
                return realmRef = RealmScene.Session;
            }
            return session;
        }
    }

    private void OnRealmConnected()
    {
        Log.Debug("已连接到Realm服务器");
    }

    private void OnGateConnected()
    {
        // 挂载心跳组件，设置每隔3000毫秒发送一次心跳给服务器
        // 只需要给客户端保持连接的服务器挂心跳
        GateScene.Session.AddComponent<SessionHeartbeatComponent>().Start(3000);
        Log.Debug("已连接到网关服务器");
    }

    private void OnConnectFail()
    {
        Log.Error("无法连接到服务器");
    }
}