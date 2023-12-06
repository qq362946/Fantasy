using Fantasy;

namespace BestGame;

public class GateAccountDestroySystem : DestroySystem<GateAccount>
{
    protected override void Destroy(GateAccount self)
    {
        self.AuthName = null;
        self.Roles.Clear();

        self.SessionRumtimeId = 0;
        self.RoleDic.Clear();
        self.SelectRoleId = 0;
        self.RegisterTime = 0;
        self.LoginedGate = false;
        self.MapSceneIds.Clear();
    }
}

/// 网关帐号状态,角色管理
/// Dictionary<int, uint> MapSceneIds缓存,此网关帐号登录某地图,哪个MapScene的记录
public static class GateAccountSystem
{
    // 玩家登录网关后获取角色列表时，在GateAccount缓存Role
    public static async FTask<Dictionary<long, Role>> GetRoles(this GateAccount self)
    {
        if (self.Roles.Count == 0 || self.RoleDic.Count > 0)
        {
            return self.RoleDic;
        }

        List<Role> roles = await self.GetDB().Query<Role>(b => self.Roles.Contains(b.Id));

        if (roles != null)
        {
            foreach (Role role in roles)
            {
                self.AddRole(role);
            }
        }

        return self.RoleDic;
    }

    public static async FTask CreateRole(this GateAccount self, Role role)
    {
        self.Roles.Add(role.Id);

        self.AddRole(role);

        var db = self.GetDB();
        await db.Save(role);
        await db.Save(self);
    }

    public static async FTask SaveRole(this GateAccount self, Role role)
    {
        await self.GetDB().Save(role);
    }

    public static void AddRole(this GateAccount self, Role role)
    {
        if (!self.RoleDic.ContainsKey(role.Id))
        {
            self.RoleDic.Add(role.Id, role);
        }
    }

    public static Role GetRole(this GateAccount self, long roleId)
    {
        self.RoleDic.TryGetValue(roleId, out Role role);

        return role;
    }

    public static Role GetCurRole(this GateAccount self)
    {
        return self.GetRole(self.SelectRoleId);
    }

    /// 根据SessionRumtimeId获取网关session
    public static bool TryGeySession(this GateAccount self, out Session session)
    {
        session = default;
        long rumtimeId = self.SessionRumtimeId;

        if (rumtimeId != 0)
        {
            if (!Entity.TryGetEntity(rumtimeId, out var entity))
            {
                return false;
            }

            session = (Session) entity;
            return true;
        }

        session = null;
        return false;
    }

    public static IDateBase GetDB(this GateAccount self)
    {
        if (self.TryGeySession(out Session session))
            return session.Scene.World.DateBase;
        return null;
    }

    public static SceneConfig GetMapScene(this GateAccount self, int mapNum, uint zoneId)
    {
        var mapSceneId = self.GetMapSceneId(mapNum);
        if (mapSceneId == 0)
        {
            // 如果没有登录此地图的记录，随机选择一个地图配置
            var mapScene = SceneHelper.GetSceneRandom(SceneType.Map, zoneId);
            // 不用存库，在维护周期内在GateAccount记住此地图登录的是这个mapScene
                // 这是指世界地图，不是副本。副本需要存库，因为副本是动态创建的，另外通常副本有专门的副本服务器。
            self.MapSceneIds.Add(mapNum, mapScene.Id);
            return mapScene;
        }
        return SceneConfigData.Instance.Get(mapSceneId);
    }

    public static uint GetMapSceneId(this GateAccount self,int mapNum)
    {
        if (!self.MapSceneIds.TryGetValue(mapNum, out uint mapSceneId))
        {
            return 0;
        }

        return mapSceneId;
    }

    /// 网关账号状态
    public static bool IsOnline(this GateAccount self)
    {
        foreach (KeyValuePair<long, Role> kv in self.RoleDic)
        {
            if (kv.Value.State == RoleState.Online)
            {
                return true;
            }
        }

        return false;
    }
}