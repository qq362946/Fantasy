# Entity / Component 操作

## 创建 Entity

```csharp
var player = Entity.Create<Player>(scene, isPool: true, isRunEvent: true);
var player = Entity.Create<Player>(scene, id: playerId, isPool: true, isRunEvent: true);
var entity = Entity.Create(scene, typeof(Player), isPool: true, isRunEvent: true);
```

参数含义：

- `isPool: true` - 从对象池创建，推荐
- `isRunEvent: true` - 触发 `AwakeSystem`

## 组件操作

```csharp
var health = player.AddComponent<HealthComponent>();
var health = player.AddComponent<HealthComponent>(id: 10001);

var health = player.GetComponent<HealthComponent>();
var health = player.GetOrAddComponent<HealthComponent>();

if (player.HasComponent<HealthComponent>()) { }

player.RemoveComponent<HealthComponent>();
player.RemoveComponent<HealthComponent>(isDispose: false);
```

## 访问父实体

```csharp
Entity parent = health.Parent;
Player typedParent = health.GetParent<Player>();
```

## 多实例组件

默认每种类型只能有一个实例。需要多个同类型组件时，实现 `ISupportedMultiEntity`。

```csharp
public sealed class BuffComponent : Entity, ISupportedMultiEntity
{
    public int BuffId;
    public float Duration;
}

var buff1 = player.AddComponent<BuffComponent>();
var buff2 = player.AddComponent<BuffComponent>();

var buff = player.GetComponent<BuffComponent>(buffId);

foreach (var buff in player.ForEachMultiEntity)
{
    Log.Info($"Buff ID: {buff.Id}");
}

player.RemoveComponent<BuffComponent>(buffId);
```

## 相关文档

- `entity-definition.md` - Entity / Component 定义方式
- `object-pool.md` - 对象池与销毁规则
- `scene.md` - Scene 归属和生命周期边界
