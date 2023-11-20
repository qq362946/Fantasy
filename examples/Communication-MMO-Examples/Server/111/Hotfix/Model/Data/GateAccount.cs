using MongoDB.Bson.Serialization.Attributes;
using Fantasy;

namespace BestGame;

/// <summary>
/// 区账号,管理区服下的游戏角色
/// </summary>
public class GateAccount : Entity
{
    public string AuthName;

    [BsonIgnore] 
    public long RegisterTime; 

    // 角色Id
    public List<long> Roles = new List<long>();

    [BsonIgnore]
    public Dictionary<long, Role> RoleDic = new Dictionary<long, Role>();

    [BsonIgnore]
    public readonly Dictionary<int, uint> MapSceneIds = new Dictionary<int, uint>();

    [BsonIgnore]
    public long SelectRoleId;

    [BsonIgnore]
    public bool LoginedGate;

    [BsonIgnore]
    public long SessionRumtimeId;

    [BsonIgnore]
    public long AddressableId;
}