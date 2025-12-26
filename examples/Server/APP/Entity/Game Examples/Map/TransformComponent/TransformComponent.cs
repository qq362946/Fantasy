using System.Numerics;
using Fantasy.Entitas;
using LightProto;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy;

public sealed class TransformComponent : Entity
{
    public Vector3 Position;
    public Quaternion Rotation;

    [BsonIgnore]
    [MemoryPackIgnore]
    [ProtoIgnore]
    public readonly Position TempPosition = new Position();
}