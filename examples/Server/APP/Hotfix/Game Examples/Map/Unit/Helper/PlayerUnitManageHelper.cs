using System.Runtime.CompilerServices;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public static class PlayerUnitManageHelper
{
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
                
                var m2CPalyerJoin = M2C_PalyerJoin.Create();
                m2CPalyerJoin.Unit = playerUnit.ToProtocol();
                linkTerminus.Send(m2CPalyerJoin);
                return;
            }
            case AddPlayerUnitNotify.SyncEveryone:
            {
                SendPlayerJoinToEveryone(playerUnit, playerUnitManageComponent);
                return;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(notify), notify, null);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPlayerJoinToEveryone(PlayerUnit playerUnit, PlayerUnitManageComponent? playerUnitManageComponent = null)
    {
        playerUnitManageComponent ??= playerUnit.Scene.GetComponent<PlayerUnitManageComponent>();

        foreach (var (_, unit) in playerUnitManageComponent.Units)
        {
            if (!unit.TryGetLinkTerminus(out var linkTerminus))
            {
                continue;
            }

            var m2CPalyerJoin = M2C_PalyerJoin.Create();
            m2CPalyerJoin.Unit = playerUnit.ToProtocol();
            linkTerminus.Send(m2CPalyerJoin);
        }
    }
}