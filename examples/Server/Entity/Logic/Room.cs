using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy
{
    public class Room :Entity
    {
        public long roomId;
        List<Unit> units = new List<Unit>();
        bool isStarted = false;

        long sortID = 0;
        public long GetNextSortId { get { if (sortID == long.MaxValue) sortID = 0;return sortID++; }  }

        public Unit GetUnit(long id) => units.Find((u) => u.Id == id);
        public Unit GetUnitByClientId(long clientId) => units.Find((u) => u.ClientID == clientId);

        public void AddUnit(Unit unit)
        {
            if (units.Contains(unit) == false)
            {
                units.Add(unit);
                if (units.Count == 2&&isStarted==false)
                {
                    isStarted = true;
                    Log.Debug($"房间:{roomId}人数已满，一秒后开始游戏");
                    StartGame();
                }
            }
        }
        async void StartGame()
        {
            await Task.Delay(1000);
            void InitData(M2G_StartGame message, long id) => message.ClientID = id;
            this.M_Broadcast<M2G_StartGame>(InitData);
        }
        public void RemoveUnit(long clientId)
        {
            var unit = units.Find(u => u.ClientID == clientId);
            if (unit != null)
            {
                units.Remove(unit);
                //unit.Dispose();
            }
            if (units.Count == 0 && isStarted == true)
            {
                this.Scene.GetComponent<LogicMgr>().RemoveRoom(this);
            }
        }
        public List<long> AllClientID()
        {
            return units.Select(u => u.ClientID).ToList();
        }
        public List<InitData> GetAllNeedInit()
        {
            var list = new List<InitData>();
            for (int i = 0; i < units.Count; i++)
            {
                list.AddRange(units[i].GetAllGameEntites().Select(u => u.ToData()));
            }
            return list;
        }
        public async FTask<long> AddGameEntity(Unit unit, long prefabId, List<long> scriptsID, TransformData transform)
        {
            var ge = GameEntity.Create<GameEntity>(this.Scene, false, false);
            ge.AddComponent<GameBody>();
            ge.clientID = unit.ClientID;
            ge.prefabID = prefabId;
            ge.transformData = transform;
            ge.MaskSure(scriptsID);
            await ge.AddComponent<AddressableMessageComponent>().Register();
            unit.AddGameEntity(ge);
            var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate)[0];
            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i] != unit)
                {
                    this.Scene.NetworkMessagingComponent.SendInnerRoute(sceneConfig.RouteId, new M2G_CreateNetworkObjectId()
                    {
                        ClientID = units[i].ClientID,
                        data = ge.ToData()
                    });
                }

            }
            //unit.Scene.GetSession()
            return ge.Id;
        }
        public Unit[] GetAllUnits()=>units.ToArray();
        #region 房间内的逻辑


        #endregion
    }
}
