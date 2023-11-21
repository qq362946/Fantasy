using Fantasy;

namespace BestGame;

public enum ServerState
{
    None = 0,
    Maintenance, // 维护中
    Start, // 开服
    Close, // 关服
}

public class GateInfo : Entity
{
    public uint ConfigId; // 配置Id
    public string OutAddress; // 外网地址
    public uint World;
    public uint ServerId; // 路由Id
}

public class Zone : Entity
{
    public uint World;
    public List<GateInfo> Gates = new List<GateInfo>();
    public ServerState ServerState;
}

/// 账号登录注册使用的账号认证组件
/// 包含初始化区服的网关列表信息，查询与缓存登录账号，保存注册账号等方法
public class AccountManager : Entity
{
    // 账号列表
    public Dictionary<string, Account> AccountDic = new Dictionary<string, Account>();
    // 缓存Zone的字典
    public Dictionary<uint, Zone> ZoneDic = new Dictionary<uint, Zone>();
    // 用于排序，供获取区服列表
    public List<Zone> ZoneList = new List<Zone>();

    public IDateBase GetDB()
    {
        return this.Scene.World.DateBase;
    }

    public Account GetAccountInCache(string authName)
    {
        AccountDic.TryGetValue(authName, out Account account);

        return account;
    }

    // 理解获取帐号的缓存逻辑
    public async FTask<Account> GetAccount(string authName, bool force = false)
    {
        var _authAccountDBLock = new CoroutineLockQueueType("AuthAccountDBLock");
        using (await _authAccountDBLock.Lock(authName.GetHashCode()))
        {
            Account account = null;

            if (!force)
            {
                // 从缓存取得账号
                if (AccountDic.TryGetValue(authName, out account))
                    return account;
            }
            else
            {
                // 强制移除旧的账号
                if (AccountDic.TryGetValue(authName, out account))
                    account.Dispose();
                    AccountDic.Remove(authName);
            }

            // 从数据库查询账号
            var tableName = Account.TableName(authName);
            var v = await GetDB().Query<Account>(b => b.AuthName == authName, tableName);
            if (v == null || v.Count == 0) return null; 

            // 缓存账号
            account = v[0];
            AccountDic.Add(authName, account);

            return account;
        }
    }

    public async FTask SaveAccount(Account account)
    {
        await GetDB().Save(account, account.TableName());
    }
}
