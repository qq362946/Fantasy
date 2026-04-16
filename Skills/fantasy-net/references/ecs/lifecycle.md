# ECS 生命周期 System

System 负责响应 Entity 的生命周期事件，与 Entity 数据定义分离，放在 Hotfix 或逻辑层，由 Source Generator 编译时自动注册，无需手动注册。

---

## Workflow

```
需要响应哪个事件？
│
├─► Entity 创建时初始化 ─────────────────────────────► AwakeSystem
│
├─► 每帧持续执行逻辑 ────────────────────────────────► UpdateSystem
│
├─► Entity 销毁时清理资源 ───────────────────────────► DestroySystem
│
├─► 从数据库加载后重建缓存/状态 ─────────────────────► DeserializeSystem
│
├─► 跨服传送前保存/注销 ─────────────────────────────► TransferOutSystem
│
└─► 跨服传送后恢复/重注册 ───────────────────────────► TransferInSystem
```

---

## AwakeSystem

Entity 创建时触发（`Entity.Create` 中 `isRunEvent: true` 时）：

```csharp
public sealed class PlayerAwakeSystem : AwakeSystem<Player>
{
    protected override void Awake(Player self)
    {
        self.Level = 1;
        Log.Debug($"Player {self.Id} created");
    }
}
```

---

## UpdateSystem

每帧触发，适合持续性逻辑（如技能冷却、AI tick）：

```csharp
public sealed class PlayerUpdateSystem : UpdateSystem<Player>
{
    protected override void Update(Player self)
    {
        // 每帧逻辑
    }
}
```

---

## DestroySystem

Entity 销毁时触发，适合清理订阅、释放资源。**若 Entity 使用对象池（`isPool: true`），必须在此将所有自定义字段重置为默认值**，否则对象被池回收后再次取出时会携带脏数据：

```csharp
public sealed class PlayerDestroySystem : DestroySystem<Player>
{
    protected override void Destroy(Player self)
    {
        // 重置自定义字段
        self.Name = null;
        self.Level = 0;
        self.Gold = 0;
        Log.Debug($"Player {self.Id} destroyed");
    }
}
```

---

## DeserializeSystem

Entity 实现了 `ISupportedSerialize` 接口，且反序列化后需要重建运行时状态时使用（如重建缓存索引、恢复计时器引用）：

```csharp
public sealed class PlayerDeserializeSystem : DeserializeSystem<Player>
{
    protected override void Deserialize(Player self)
    {
        // 例：重建背包索引、恢复计时器引用
    }
}
```

**与 AwakeSystem 的区别：**
- `Awake` — 全新创建时执行（`isRunEvent: true`）
- `Deserialize` — 反序列化后执行（`isRunEvent: false` 场景）

---

## TransferOutSystem / TransferInSystem

Entity 关联了漫游 Terminus 并跨服传送时才需要。均为纯异步接口：

```csharp
// 传送发起前：保存状态、注销订阅、清理当前场景资源
public sealed class PlayerTransferOutSystem : TransferOutSystem<Player>
{
    protected override async FTask Out(Player self)
    {
        await self.GetComponent<PlayerDataComponent>().Save();
        Log.Debug($"Player {self.Id} leaving scene");
    }
}

// 传送到达后：重新初始化、重注册订阅、加载目标场景数据
public sealed class PlayerTransferInSystem : TransferInSystem<Player>
{
    protected override async FTask In(Player self)
    {
        await self.GetComponent<PlayerDataComponent>().Load();
        Log.Debug($"Player {self.Id} arrived in new scene");
    }
}
```

框架在漫游传送流程中调用：
```csharp
await scene.EntityComponent.TransferOut(player);  // 传送前
await scene.EntityComponent.TransferIn(player);   // 传送后
```

> 传送 System 仅服务器端使用，Unity 客户端不涉及。

---

## 生命周期顺序总结

```
Entity.Create (isRunEvent: true)
  └─► AwakeSystem

每帧
  └─► UpdateSystem

entity.Dispose()
  └─► DestroySystem（级联销毁所有子 Entity）

数据库加载 (isRunEvent: false)
  └─► DeserializeSystem

跨服传送
  ├─► TransferOutSystem（传送前，当前服务器）
  └─► TransferInSystem（传送后，目标服务器）
```
