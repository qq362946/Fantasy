using System.Runtime.CompilerServices;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy;

/// <summary>
/// 玩家单位管理辅助类，提供玩家单位的添加、同步和广播功能
/// </summary>
public static class PlayerUnitManageHelper
{
    /// <summary>
    /// 添加玩家单位时的通知方式
    /// </summary>
    public enum AddPlayerUnitNotify
    {
        /// <summary>
        /// 不通知
        /// </summary>
        NoNotification,
        /// <summary>
        /// 通知自己
        /// </summary>
        SyncSelf,
        /// <summary>
        /// 通知所有人
        /// </summary>
        SyncEveryone
    }
    
    /// <summary>
    /// 添加玩家单位时的通知方式
    /// </summary>
    public enum RemovePlayerUnitNotify
    {
        /// <summary>
        /// 不通知
        /// </summary>
        NoNotification,
        /// <summary>
        /// 通知自己
        /// </summary>
        SyncSelf,
        /// <summary>
        /// 通知所有人
        /// </summary>
        SyncEveryone
    }

    /// <summary>
    /// 添加玩家单位到场景管理组件，并根据通知方式同步给客户端
    /// </summary>
    /// <param name="playerUnit">要添加的玩家单位</param>
    /// <param name="notify">通知方式：不通知、仅通知自己、通知所有人</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPlayerUnit(PlayerUnit playerUnit, AddPlayerUnitNotify notify)
    {
        var playerUnitManageComponent = playerUnit.Scene.GetComponent<PlayerUnitManageComponent>();

        playerUnitManageComponent.Add(playerUnit);

        switch (notify)
        {
            case AddPlayerUnitNotify.NoNotification:
            {
                return;
            }
            case AddPlayerUnitNotify.SyncSelf:
            {
                if (!playerUnit.TryGetLinkTerminus(out var linkTerminus))
                {
                    return;
                }

                var unitCreateMessage = M2C_UnitCreate.Create();
                unitCreateMessage.Unit = playerUnit.ToProtocol(true);
                unitCreateMessage.IsSelf = true;
                linkTerminus.Send(unitCreateMessage);
                return;
            }
            case AddPlayerUnitNotify.SyncEveryone:
            {
                BroadcastUnitCreate(playerUnit, playerUnitManageComponent);
                return;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(notify), notify, null);
            }
        }
    }

    /// <summary>
    /// 在单位场景中移除一个PlayerUnit，并根据通知方式同步给客户端
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="playerUnitId"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemovePlayerUnit(Scene scene, long playerUnitId)
    {
        // var playerUnitManageComponent = scene.GetComponent<PlayerUnitManageComponent>();
        //
        // switch (notify)
        // {
        //     case RemovePlayerUnitNotify.NoNotification:
        //     {
        //         return;
        //     }
        //     case RemovePlayerUnitNotify.SyncSelf:
        //     {
        //         return;
        //     }
        //     case RemovePlayerUnitNotify.SyncEveryone:
        //     {
        //         return;
        //     }
        // }
    }

    /// <summary>
    /// 向场景内所有玩家广播单位创建消息（不包括单位自己）
    /// </summary>
    /// <param name="playerUnit">要广播的玩家单位</param>
    /// <param name="playerUnitManageComponent">玩家单位管理组件（可选，为null时自动获取）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BroadcastUnitCreate(PlayerUnit playerUnit, PlayerUnitManageComponent? playerUnitManageComponent = null)
    {
        var unitCreateMessage = M2C_UnitCreate.Create(false);

        try
        {
            unitCreateMessage.Unit = playerUnit.ToProtocol(true);
            unitCreateMessage.IsSelf = false;  // 广播给其他人，标记为非自己
            BroadcastToAllPlayers(playerUnit.Scene, unitCreateMessage, playerUnitManageComponent);
        }
        finally
        {
            unitCreateMessage.Return();
        }
    }

    /// <summary>
    /// 向场景内除指定玩家外的其他玩家广播单位创建消息
    /// </summary>
    /// <param name="scene">场景对象</param>
    /// <param name="unitInfo">要广播的单位信息</param>
    /// <param name="skipPlayerId">要跳过的玩家ID（通常是单位的所有者）</param>
    /// <param name="playerUnitManageComponent">玩家单位管理组件（可选，为null时自动获取）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BroadcastUnitCreate(Scene scene, UnitInfo unitInfo, long skipPlayerId, PlayerUnitManageComponent? playerUnitManageComponent = null)
    {
        var unitCreateMessage = M2C_UnitCreate.Create(false);
        playerUnitManageComponent ??= scene.GetComponent<PlayerUnitManageComponent>();

        try
        {
            unitCreateMessage.Unit = unitInfo;
            unitCreateMessage.IsSelf = false;  // 广播给其他人，标记为非自己

            foreach (var (unitId, unit) in playerUnitManageComponent.Units)
            {
                // 跳过指定玩家
                if (unitId == skipPlayerId)
                {
                    continue;
                }

                if (!unit.TryGetLinkTerminus(out var linkTerminus))
                {
                    continue;
                }

                linkTerminus.Send(unitCreateMessage);
            }
        }
        finally
        {
            unitCreateMessage.Return();
        }
    }

    /// <summary>
    /// 向客户端发送单位创建消息
    /// </summary>
    /// <param name="linkTerminus">目标客户端连接</param>
    /// <param name="unitInfo">单位信息</param>
    /// <param name="isSelf">是否是接收者自己的单位</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendUnitCreate(Terminus linkTerminus, UnitInfo unitInfo, bool isSelf)
    {
        var unitCreateMessage = M2C_UnitCreate.Create();
        unitCreateMessage.Unit = unitInfo;
        unitCreateMessage.IsSelf = isSelf;
        linkTerminus.Send(unitCreateMessage);
    }

    /// <summary>
    /// 将场景内所有单位信息同步给指定玩家（常用于新玩家加入场景时）
    /// </summary>
    /// <param name="playerUnit">目标玩家单位</param>
    /// <param name="playerUnitManageComponent">玩家单位管理组件（可选，为null时自动获取）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SyncAllUnitsToPlayer(PlayerUnit playerUnit, PlayerUnitManageComponent? playerUnitManageComponent = null)
    {
        if (!playerUnit.TryGetLinkTerminus(out var linkTerminus))
        {
            Log.Error($"PlayerUnit:{playerUnit.Id} not link terminus");
            return;
        }

        var unitCreateMessage = M2C_UnitCreate.Create(false);
        playerUnitManageComponent ??= playerUnit.Scene.GetComponent<PlayerUnitManageComponent>();

        try
        {
            foreach (var (_, unit) in playerUnitManageComponent.Units)
            {
                unitCreateMessage.Unit = unit.ToProtocol(true);
                linkTerminus.Send(unitCreateMessage);
            }
        }
        finally
        {
            unitCreateMessage.Return();
        }
    }

    /// <summary>
    /// 将场景内除指定单位外的其他单位信息同步给目标客户端（常用于新玩家加入场景时，避免重复同步自己）
    /// </summary>
    /// <param name="linkTerminus">目标客户端连接</param>
    /// <param name="scene">场景对象</param>
    /// <param name="skipUnitId">要跳过的单位ID（通常是接收者自己的单位ID）</param>
    /// <param name="playerUnitManageComponent">玩家单位管理组件（可选，为null时自动获取）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SyncOtherUnits(Terminus linkTerminus, Scene scene, long skipUnitId, PlayerUnitManageComponent? playerUnitManageComponent = null)
    {
        var unitCreateMessage = M2C_UnitCreate.Create(false);
        playerUnitManageComponent ??= scene.GetComponent<PlayerUnitManageComponent>();

        try
        {
            unitCreateMessage.IsSelf = false;  // 同步的是其他人的单位

            foreach (var (unitId, unit) in playerUnitManageComponent.Units)
            {
                // 跳过指定单位
                if (unitId == skipUnitId)
                {
                    continue;
                }

                unitCreateMessage.Unit = unit.ToProtocol(true);
                linkTerminus.Send(unitCreateMessage);
            }
        }
        finally
        {
            unitCreateMessage.Return();
        }
    }

    /// <summary>
    /// 向场景内所有玩家广播指定消息（通用广播方法）
    /// </summary>
    /// <typeparam name="T">消息类型，必须实现IRoamingMessage接口</typeparam>
    /// <param name="scene">场景对象</param>
    /// <param name="message">要广播的消息</param>
    /// <param name="playerUnitManageComponent">玩家单位管理组件（可选，为null时自动获取）</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BroadcastToAllPlayers<T>(Scene scene, T message, PlayerUnitManageComponent? playerUnitManageComponent = null) where T : IRoamingMessage
    {
        playerUnitManageComponent ??= scene.GetComponent<PlayerUnitManageComponent>();

        foreach (var (_, unit) in playerUnitManageComponent.Units)
        {
            if (!unit.TryGetLinkTerminus(out var linkTerminus))
            {
                continue;
            }

            linkTerminus.Send(message);
        }
    }
}