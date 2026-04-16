# Terminus 传送

## 发起传送

```csharp
var errorCode = await mapPlayer.StartTransfer(targetSceneAddress);
if (errorCode != 0)
{
    mapPlayer.Send(new Map2C_TransferFailedNotification { ErrorCode = errorCode });
    return;
}
```

也可以通过 `Terminus` 发起：

```csharp
if (!mapPlayer.TryGetLinkTerminus(out var terminus)) return;
errorCode = await terminus.StartTransfer(targetSceneAddress);
```

## 传送生命周期

由 Source Generator 自动注册，无需手动注册。

```csharp
public sealed class MapPlayerTransferOutSystem : TransferOutSystem<MapPlayer>
{
    protected override async FTask Out(MapPlayer self)
    {
        await self.SaveToDatabase();
        self.Send(new Map2C_TransferStartNotification { TargetMapId = self.TargetMapId });
    }
}

public sealed class MapPlayerTransferInSystem : TransferInSystem<MapPlayer>
{
    protected override async FTask In(MapPlayer self)
    {
        await self.LoadMapData(self.Scene);
        self.SetSpawnPosition();
        self.Send(new Map2C_TransferCompleteNotification
        {
            MapId = self.Scene.SceneConfig.Id,
            Position = self.Position
        });
    }
}
```

## 内部流程

```text
原服务器: TransferOut -> 锁定 Terminus -> 序列化并发送
目标服务器: 反序列化恢复 -> 解锁 Terminus -> TransferIn
原服务器: 销毁旧 Terminus 和旧实体 -> 触发 OnDisposeTerminus
```

## 必记规则

1. 传送成功后原实例已被销毁，不要继续使用旧实例
2. 其他组件若持有旧引用，应该提前记录 ID，传送后重新查找
3. 传送失败时 Terminus 自动解锁
4. 传送后的清理逻辑见 `on-dispose-terminus.md`
