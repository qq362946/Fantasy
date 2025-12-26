using System.Numerics;
using System.Runtime.CompilerServices;
using Fantasy.Entitas;
using Fantasy.Network.Roaming;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy;

public static class PlayerUnitFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlayerUnit CreatePlayer(Scene scene, string accountName)
    {
        var playerUnit = Entity.Create<PlayerUnit>(scene);
        // 基础信息
        playerUnit.Name = accountName;
        playerUnit.UnitType = UnitType.Player;
        // 挂载组件
        var transformComponent = playerUnit.AddComponent<TransformComponent>();
        transformComponent.Position = new Vector3(6.22f, 0, -18.64f);
        playerUnit.Transform = transformComponent;
        return playerUnit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlayerUnit CreateMonster(Scene scene, string accountName, Vector3 position)
    {
        // 需要自己添加，这里只是一个例子，表示每个类型都单独创建一个方法来做
        return null!;
    }
}