using Fantasy.Entitas;
using LightProto;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy;

public enum UnitType
{
    None = 0,
    Player = 1,
    Monster = 2,
    Boss = 3,
    Npc = 4,
}

/// <summary>
/// 其实这个应该是Unit,因为框架的某一个例子用了Unit,所以改名为PlayerUnit
/// </summary>
public sealed class PlayerUnit : Entity
{
    public string Name;
    public UnitType UnitType;
    
    [BsonIgnore]
    [MemoryPackIgnore]
    [ProtoIgnore]
    public TransformComponent Transform;
}