using Fantasy;

namespace BestGame;

public class AccountManagerDestroySystem : DestroySystem<AccountManager>
{
    protected override void Destroy(AccountManager self)
    {
        self.ZoneDic.Clear();
        self.ZoneList.Clear();
        self.AccountDic.Clear();
    }
}

public static class AccountManagerSystem
{
    /// 初始化网关列表与更新网关访问状态
    public static async FTask MachineStartFinished(this AccountManager self)
    {
        using (OneToManyListPool<uint, GateInfo> dic = OneToManyListPool<uint, GateInfo>.Create())
        {
            var list  = WorldConfigData.Instance.List;
            /// 区服网关数据添加到字典。
            /// 一个区服一个数据库，以world为基准进行遍历
            foreach(WorldConfig world in list)
            {
                foreach(SceneConfig gate in SceneHelper.GetSceneByWorld(world.Id,SceneType.Gate)){
                    GateInfo info = Entity.Create<GateInfo>(self.Scene);
                    info.World = world.Id;
                    info.OutAddress = SceneHelper.GetOutAddressByServer(gate.ServerConfigId);
                    info.ConfigId = gate.Id;
                    info.ServerId = gate.ServerConfigId;
                    
                    dic.Add(world.Id, info);
                }
            }
            
            /// 遍历区服网关，连接网关服务器状态
            foreach (KeyValuePair<uint, List<GateInfo>> kv in dic)
            {
                uint world = kv.Key;
                List<GateInfo> gateInfos = kv.Value;

                if (self.ZoneDic.TryGetValue(world, out Zone zone))
                {
                    zone.Dispose();
                    self.ZoneDic.Remove(world);
                }

                zone = Entity.Create<Zone>(self.Scene);
                self.ZoneDic.Add(world, zone);
                zone.Gates.AddRange(gateInfos);

                Log.Info($"鉴权 连接 {world} Gate");
                using (ListPool<FTask<IResponse>> rplist = ListPool<FTask<IResponse>>.Create())
                {
                    foreach (var gateInfo in gateInfos)
                    {
                        var sceneConfig = SceneConfigData.Instance.Get(gateInfo.ConfigId);
                        var v = MessageHelper.CallInnerServer(self.Scene,gateInfo.ServerId, new S2S_ConnectRequest()
                        {
                            Key = (int)gateInfo.ServerId
                        });
                        rplist.Add(v);
                    }
                    await FTask<IResponse>.WhenAll(rplist);
                }
                Log.Info($"鉴权 连接 {world} Gate 完成");
                
                // 设置Start状态
                zone.ServerState = ServerState.Start;
                zone.World = world;
            }
            
            self.ZoneList.Clear();
            self.ZoneList.AddRange(self.ZoneDic.Values);
            self.ZoneList.Sort((a, b) => (int)a.World - (int)b.World);
        }
        Log.Info($"鉴权更新Gate数据，完整区服信息 {self.ZoneList.ToJson()}");
        
        await FTask.CompletedTask;
    }
}