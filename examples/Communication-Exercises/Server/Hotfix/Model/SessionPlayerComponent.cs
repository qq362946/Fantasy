using Fantasy;
namespace BestGame;

/// 这是一个在网关缓存玩家基本信息的组件，添加给客户端与网关的连接session
public class SessionPlayerComponent : Entity
{
    /// 玩家playerId
    public long playerId;
    
    /// 在网关缓存一个AddressableId
    public long AddressableId;
}