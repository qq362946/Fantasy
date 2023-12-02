using MongoDB.Bson.Serialization.Attributes;
using UnityEngine;
using BestGame;

namespace Fantasy;

public class Unit : Entity
{
    /// 客户端与网关连接SessionRuntimeId
    public long SessionRuntimeId;

    // 网关路由id
    public long GateRouteId;

    public UnitType UnitType;

    public RoleInfo RoleInfo;

    /// 下线状态
    public OnlineState unitState = OnlineState.None;

    /// 移动状态
    public MoveState moveState;

    [BsonElement("Position")]
    private Vector3 _position;
    [BsonIgnore]
    public Vector3 Position
    {
        get => _position;
        set
        {
            Vector3 p = _position;
                
            _position = value;
                
            // EventSystem.Publish(new EventSystemStructHandler.ChangePosition
            // {
            //     Unit = this,
            //     OldPos = p,
            //     NewPos = value
            // });
        }
    }
    
    [BsonElement("Rotation")]
    private Quaternion _rotation;
    [BsonIgnore]
    public Quaternion Rotation { 
        get => _rotation;
        set
        {
            _rotation = value;
        } 
    }

    
}

public enum OnlineState
{
    None = 0,
    NoOffline = 1, // 不下线
    CheckStay = 2, // 检测挂机
    ForceOffline = 3, // 强制下线
    DelayOffline = 4 // 延迟下线中
}

public enum UnitType
{
    None = 0,
    Player = 1,
    Monster = 2,
    NPC = 3
}