# Struct 事件完整流程

**适用场景：** 频繁触发（伤害、移动、状态变化），数据简单，需要零 GC。

---

## 第 1 步：定义 Struct 事件

```csharp
public struct {事件名}Event
{
    // 根据业务需求定义字段，用于传递事件参数
}
```

**特点：** 栈上分配，零 GC，适合每秒触发数百次的事件。

---

## 第 2 步：创建监听器

### 同步监听器（快速执行、无 IO）

```csharp
using Fantasy.Event;

public class On{事件名} : EventSystem<{事件名}Event>
{
    protected override void Handler({事件名}Event self)
    {        
        // 业务逻辑
    }
}
```

**适用场景：** UI 更新、简单计算、状态修改。

**注意：** 

- 一个事件可以有多个监听器，同步监听器顺序执行，异步监听器并行执行
- 监听器之间不应有执行顺序依赖

---

## 第 3 步：发布事件

### 同步发布

```csharp
scene.EventComponent.Publish(new {事件名}Event
{
    // 根据事件定义的字段赋值
    PlayerId = player.Id,
    Value = 100
});
```

---

### 异步发布（等待所有异步监听器完成）

```csharp
await scene.EventComponent.PublishAsync(new {事件名}Event
{
    // 根据事件定义的字段赋值
    PlayerId = player.Id,
    Value = 10
});
```

---

## 完整示例

```csharp
// 1. 定义事件
public struct PlayerDamageEvent
{
    public long AttackerId;
    public long TargetId;
    public int Damage;
    // 可以在事件里传递一个已存在的实体
    public Player Player;
}

// 2. 创建同步监听器
public class OnPlayerDamage : EventSystem<PlayerDamageEvent>
{
    protected override void Handler(PlayerDamageEvent self)
    {
        var player = self.Player;
        player.Health -= self.Damage;
        
        if (player.Health <= 0)
        {
            self.Scene.EventComponent.Publish(new PlayerDeathEvent
            {
                PlayerId = self.TargetId,
                KillerId = self.AttackerId
            });
        }
    }
}

// 3. 创建异步监听器
public class LogDamageAsync : AsyncEventSystem<PlayerDamageEvent>
{
    protected override async FTask Handler(PlayerDamageEvent self)
    {
        // 异步记录伤害日志到数据库
        await SaveDamageLogToDatabase(self.AttackerId, self.TargetId, self.Damage);
    }
    
    private async FTask SaveDamageLogToDatabase(long attackerId, long targetId, int damage)
    {
        await FTask.CompletedTask;
    }
}

// 4. 同步发布事件
scene.EventComponent.Publish(new PlayerDamageEvent
{
    AttackerId = attacker.Id,
    TargetId = target.Id,
    Damage = 100,
    Player = player
});

// 5. 异步发布事件（等待所有异步监听器完成）
await scene.EventComponent.PublishAsync(new PlayerDamageEvent
{
    AttackerId = attacker.Id,
    TargetId = target.Id,
    Damage = 100,
    Player = player
});
```

---

## 注意事项

- 监听器必须是 `sealed class`，不能是泛型类
- 监听器应与 Entity 数据定义分离：多 assembly 项目放在逻辑层 assembly，单 assembly 项目按文件夹分离即可
- 不要手动注册监听器，不要修改 `.g.cs` 生成文件
- 单个监听器的错误不会影响其他监听器，框架会捕获异常并记录日志

---

## 相关文档

- `index.md` - Event 系统入口和 Workflow
- `entity-event.md` - Entity 事件完整流程
- `check-event.md` - Event 系统代码检查清单
