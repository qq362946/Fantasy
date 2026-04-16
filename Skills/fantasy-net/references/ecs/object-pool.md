# 对象池与销毁

## 对象池

```csharp
var bullet = Entity.Create<Bullet>(scene, isPool: true, isRunEvent: true);
bullet.Dispose();

bool isFromPool = bullet.IsPool();
```

最佳实践：

- 短生命周期对象 -> `isPool: true`
- 长生命周期全局对象 -> 可考虑 `isPool: false`
- 框架只自动清零自身维护的字段，自定义字段必须在 `DestroySystem` 里重置

## DestroySystem 中重置自定义字段

```csharp
public sealed class PlayerDestroySystem : DestroySystem<Player>
{
    protected override void Destroy(Player self)
    {
        self.Name = null;
        self.Level = 0;
        self.Gold = 0;
    }
}
```

如果不重置，自定义字段会在对象池复用时带上旧数据。

## 层级销毁

销毁父 Entity 会自动级联销毁所有子 Entity。

```csharp
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
var health = player.AddComponent<HealthComponent>();
var inventory = player.AddComponent<InventoryComponent>();

player.Dispose();
```

`health` 和 `inventory` 也会一起销毁。

## Scene 销毁

Scene 是更大的生命周期边界。Scene 销毁时，其下所有 Entity 都会被级联销毁。详见 `scene.md`。

## 相关文档

- `lifecycle.md` - `DestroySystem` 的完整生命周期说明
- `scene.md` - Scene 作为生命周期边界
- `entity-operations.md` - Entity 和 Component 的创建与删除
