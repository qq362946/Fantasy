using UnityEngine;
using System.Collections.Generic;
using Fantasy;

public partial class GameManager : MonoBehaviour,ISend
{
    public bool IsConnect; // 是否连接到网关服务器
    public Scene Realm;
    public Scene Gate;

    private string json = "";
    public List<RealmInfo> zoneInfo;
    public string gateAdress = "";
    public RealmInfo defaultZone;
    public RealmInfo selectZone;

    public void GameNetStart(){
        Realm = Fantasy.Entry.Initialize(); 
        Gate  = Scene.Create();

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
        return await GetRealm().Session.Call(request);
    }

    public async FTask<IResponse> Login(IRequest request)
    {
        return await GetRealm().Session.Call(request);
    }

    public Scene GetGate()
    {
        if(Gate.Session==null || Gate.Session.Id == 0)
        {
            // 建立与网关的连接，只有与网关的连接才需要挂心跳
            Gate.CreateSession(gateAdress, NetworkProtocolType.KCP, 
                OnGateConnected, OnConnectFail, null);
        }
        return Gate;
    }

    public Scene GetRealm(){
        if(Realm.Session==null || Realm.Session.Id == 0)
        {
            Realm.CreateSession(selectZone.RealmAdress, NetworkProtocolType.KCP,
                 OnRealmConnected, OnConnectFail, null);
        }
        return Realm;
    }

    public async FTask Send(IMessage message)
    {
        GetGate().Session.Send(message);
        await FTask.CompletedTask;
    }

    public async FTask Send(IAddressableRouteMessage message)
    {
        GetGate().Session.Send(message);
        await FTask.CompletedTask;
    }

    public async FTask<IResponse> Call(IRequest request)
    {
        return await GetGate().Session.Call(request);
    }

    public async FTask<IAddressableRouteResponse> Call(IAddressableRouteRequest request)
    {
        return (IAddressableRouteResponse) await GetGate().Session.Call(request);
    }

    private void OnRealmConnected()
    {
        Log.Debug("已连接到Realm服务器");
    }

    private void OnGateConnected()
    {
        IsConnect = true;
        // 挂载心跳组件，设置每隔3000毫秒发送一次心跳给服务器
        // 只需要给客户端保持连接的服务器挂心跳
        Gate.Session.AddComponent<SessionHeartbeatComponent>().Start(3000);
        Log.Debug("已连接到网关服务器");
    }

    private void OnConnectFail()
    {
        Log.Error("无法连接到服务器");
    }
}