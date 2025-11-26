# Event 系统使用指南

Event 系统是 Fantasy Framework 的核心消息机制，提供了**类型安全、高性能、零装箱**的事件发布-订阅模式，支持同步和异步两种事件处理方式。

---

## 为什么需要 Event 系统？

### 解耦与可维护性

Event 系统允许不同模块之间通过事件进行通信，而无需直接依赖：

```csharp
// ❌ 直接耦合 - 不推荐
public class Player
{
    public void LevelUp()
    {
        // 直接调用其他系统
        UIManager.ShowLevelUpEffect();
        AchievementSystem.CheckLevelAchievement(this);
        NotificationSystem.SendLevelUpNotice(this);
    }
}

// ✅ 事件解耦 - 推荐
public class Player
{
    public void LevelUp()
    {
        // 只发布事件，不关心谁在监听
        Scene.EventComponent.Publish(new PlayerLevelUpEvent
        {
            PlayerId = Id,
            NewLevel = Level
        });
    }
}
```

### 扩展性和灵活性

通过事件系统，可以在不修改原有代码的情况下，轻松添加新的功能模块：

```csharp
// 新增功能只需添加新的事件监听器
public class NewFeatureSystem : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        // 新功能逻辑
    }
}
```

---

## 核心概念

### 1. 事件数据类型

Event 系统支持两种事件数据类型：

**Struct（值类型）事件：**
```csharp
public struct PlayerLevelUpEvent
{
    public long PlayerId;
    public int OldLevel;
    public int NewLevel;
    public Scene Scene;
}
```

**优点：**
- ✅ 无 GC 分配（栈上分配）
- ✅ 适合频繁触发的轻量级事件
- ✅ 性能最佳

**Entity（引用类型）事件：**
```csharp
public class PlayerDeathEvent : Entity
{
    public long PlayerId { get; set; }
    public long KillerId { get; set; }
    public int DeathReason { get; set; }
}
```

**优点：**
- ✅ 继承自 `Entity`，可使用 ECS 生命周期
- ✅ 支持对象池（减少 GC 压力）
- ✅ 可挂载组件
- ✅ 支持自动销毁管理

### 2. 事件监听器类型

**同步事件监听器 - `EventSystem<T>`:**
```csharp
public class OnPlayerLevelUp : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        Log.Info($"玩家 {self.PlayerId} 升级到 {self.NewLevel} 级");
    }
}
```

**异步事件监听器 - `AsyncEventSystem<T>`:**
```csharp
public class OnPlayerLevelUpAsync : AsyncEventSystem<PlayerLevelUpEvent>
{
    protected override async FTask Handler(PlayerLevelUpEvent self)
    {
        Log.Info($"玩家 {self.PlayerId} 升级到 {self.NewLevel} 级");

        // 可以执行异步操作
        await SavePlayerDataToDatabase(self.PlayerId);
        await SendLevelUpRewardAsync(self.PlayerId);
    }
}
```

### 3. 自动注册机制

Event 监听器通过 **Source Generator** 在编译时自动注册，无需手动调用注册方法：

```csharp
// ✅ 只需定义监听器类，编译时自动注册
public class OnPlayerLogin : EventSystem<PlayerLoginEvent>
{
    protected override void Handler(PlayerLoginEvent self)
    {
        // 自动注册，无需手动操作
    }
}
```

**生成的代码示例：**
```csharp
// EventSystemRegistrar.g.cs (自动生成)
public class EventSystemRegistrar : IEventSystemRegistrar
{
    public RuntimeTypeHandle[] EventTypeHandles() => new[]
    {
        typeof(PlayerLoginEvent).TypeHandle,
        typeof(PlayerLevelUpEvent).TypeHandle
    };

    public IEvent[] Events() => new IEvent[]
    {
        new OnPlayerLogin(),
        new OnPlayerLevelUp()
    };
}
```

---

## 基础使用

### 1. 定义事件数据

**Struct 事件（推荐用于轻量级事件）：**
```csharp
public struct PlayerDamageEvent
{
    public long AttackerId;    // 攻击者 ID
    public long TargetId;      // 目标 ID
    public int Damage;         // 伤害值
    public int DamageType;     // 伤害类型
    public Scene Scene;        // Scene 引用（如果需要）
}
```

**Entity 事件（推荐用于复杂事件）：**
```csharp
public class ItemDropEvent : Entity
{
    public int ItemId { get; set; }
    public int ItemCount { get; set; }
    public Vector3 Position { get; set; }
    public long PlayerId { get; set; }
}
```

### 2. 创建事件监听器

**同步事件监听器：**
```csharp
using Fantasy.Event;

// 监听玩家受伤事件
public class OnPlayerDamage : EventSystem<PlayerDamageEvent>
{
    protected override void Handler(PlayerDamageEvent self)
    {
        Log.Info($"玩家 {self.TargetId} 受到来自 {self.AttackerId} 的 {self.Damage} 点伤害");

        // 更新玩家血量
        var player = self.Scene.GetEntity<Player>(self.TargetId);
        if (player != null)
        {
            player.Health -= self.Damage;

            // 检查是否死亡
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
}
```

**异步事件监听器：**
```csharp
using Fantasy.Event;
using Fantasy.Async;

// 监听物品掉落事件（需要数据库操作）
public class OnItemDropAsync : AsyncEventSystem<ItemDropEvent>
{
    protected override async FTask Handler(ItemDropEvent self)
    {
        Log.Info($"物品 {self.ItemId} x{self.ItemCount} 掉落在 {self.Position}");

        // 异步保存物品掉落记录到数据库
        await SaveDropRecordToDatabase(self.ItemId, self.PlayerId);

        // 异步通知其他服务器
        await NotifyOtherServers(self);

        Log.Info("物品掉落处理完成");
    }

    private async FTask SaveDropRecordToDatabase(int itemId, long playerId)
    {
        // 数据库操作
        await FTask.CompletedTask;
    }

    private async FTask NotifyOtherServers(ItemDropEvent eventData)
    {
        // 跨服通知
        await FTask.CompletedTask;
    }
}
```

### 3. 发布事件

**发布 Struct 事件（同步）：**
```csharp
// 创建事件数据
var damageEvent = new PlayerDamageEvent
{
    AttackerId = attacker.Id,
    TargetId = target.Id,
    Damage = 100,
    DamageType = 1,
    Scene = scene
};

// 发布同步事件
scene.EventComponent.Publish(damageEvent);
```

**发布 Struct 事件（异步）：**
```csharp
var levelUpEvent = new PlayerLevelUpEvent
{
    PlayerId = player.Id,
    OldLevel = player.Level - 1,
    NewLevel = player.Level
};

// 等待所有异步监听器处理完成
await scene.EventComponent.PublishAsync(levelUpEvent);
```

**发布 Entity 事件（同步）：**
```csharp
// 创建 Entity 事件
var itemDrop = Entity.Create<ItemDropEvent>(scene, isPool: true);
itemDrop.ItemId = 1001;
itemDrop.ItemCount = 10;
itemDrop.Position = dropPosition;
itemDrop.PlayerId = player.Id;

// 发布事件，处理完成后自动销毁 Entity
scene.EventComponent.Publish(itemDrop, isDisposed: true);

// 如果不想自动销毁，传 false
// scene.EventComponent.Publish(itemDrop, isDisposed: false);
```

**发布 Entity 事件（异步）：**
```csharp
var itemDrop = Entity.Create<ItemDropEvent>(scene, isPool: true);
itemDrop.ItemId = 1001;
itemDrop.ItemCount = 10;

// 等待所有异步监听器处理完成后，自动销毁 Entity
await scene.EventComponent.PublishAsync(itemDrop, isDisposed: true);
```

---

## 高级特性

### 1. 多个监听器处理同一事件

Event 系统支持多个监听器订阅同一事件，按注册顺序依次执行：

```csharp
// 监听器 1：UI 更新
public class OnPlayerLevelUpUI : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        Log.Info("更新 UI 显示");
        // 更新 UI
    }
}

// 监听器 2：成就检查
public class OnPlayerLevelUpAchievement : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        Log.Info("检查成就解锁");
        // 检查成就
    }
}

// 监听器 3：奖励发放
public class OnPlayerLevelUpReward : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        Log.Info("发放升级奖励");
        // 发放奖励
    }
}

// 发布事件时，所有监听器都会被调用
scene.EventComponent.Publish(new PlayerLevelUpEvent
{
    PlayerId = player.Id,
    NewLevel = player.Level
});
```

**执行顺序：**
1. `OnPlayerLevelUpUI.Handler()` → 更新 UI
2. `OnPlayerLevelUpAchievement.Handler()` → 检查成就
3. `OnPlayerLevelUpReward.Handler()` → 发放奖励

### 2. 异步事件的并行执行

所有异步监听器会**并行执行**，通过 `FTask.WaitAll()` 等待所有任务完成：

```csharp
// 监听器 1：保存数据（耗时 2 秒）
public class SaveDataOnLevelUp : AsyncEventSystem<PlayerLevelUpEvent>
{
    protected override async FTask Handler(PlayerLevelUpEvent self)
    {
        await FTask.Delay(2000);
        Log.Info("数据保存完成");
    }
}

// 监听器 2：发送通知（耗时 1 秒）
public class SendNotificationOnLevelUp : AsyncEventSystem<PlayerLevelUpEvent>
{
    protected override async FTask Handler(PlayerLevelUpEvent self)
    {
        await FTask.Delay(1000);
        Log.Info("通知发送完成");
    }
}

// 发布异步事件
var startTime = TimeHelper.Now;
await scene.EventComponent.PublishAsync(new PlayerLevelUpEvent
{
    PlayerId = player.Id,
    NewLevel = 10
});
var elapsed = TimeHelper.Now - startTime;

// 输出: 总耗时约 2000ms（并行执行，取最长耗时）
Log.Info($"总耗时: {elapsed}ms");
```

### 3. 错误处理机制

Event 系统内置异常捕获，单个监听器的错误不会影响其他监听器：

```csharp
// 监听器 1：正常执行
public class NormalHandler : EventSystem<TestEvent>
{
    protected override void Handler(TestEvent self)
    {
        Log.Info("监听器 1 执行成功");
    }
}

// 监听器 2：抛出异常
public class ErrorHandler : EventSystem<TestEvent>
{
    protected override void Handler(TestEvent self)
    {
        throw new Exception("监听器 2 发生错误！");
        // 错误会被捕获并记录日志，不会影响其他监听器
    }
}

// 监听器 3：正常执行
public class AnotherNormalHandler : EventSystem<TestEvent>
{
    protected override void Handler(TestEvent self)
    {
        Log.Info("监听器 3 执行成功");
    }
}

// 发布事件
scene.EventComponent.Publish(new TestEvent());

// 输出:
// [Info] 监听器 1 执行成功
// [Error] TestEvent Error System.Exception: 监听器 2 发生错误！
// [Info] 监听器 3 执行成功
```

**异步事件的错误处理：**
```csharp
public class AsyncErrorHandler : AsyncEventSystem<TestEvent>
{
    protected override async FTask Handler(TestEvent self)
    {
        await FTask.Delay(100);
        throw new Exception("异步监听器错误！");
    }
}

// 异步事件的错误也会被捕获
await scene.EventComponent.PublishAsync(new TestEvent());
// 其他监听器仍会正常执行
```

### 4. 热重载支持

Event 系统支持程序集热重载，当程序集重新加载时，事件监听器会自动重新注册：

```csharp
// EventComponent 实现 IAssemblyLifecycle 接口
public sealed class EventComponent : Entity, IAssemblyLifecycle
{
    // 程序集加载时注册事件
    public async FTask OnLoad(AssemblyManifest assemblyManifest)
    {
        var eventSystemRegistrar = assemblyManifest.EventSystemRegistrar;

        // 注册同步事件
        _eventMerger.Add(
            assemblyManifestId,
            eventSystemRegistrar.EventTypeHandles(),
            eventSystemRegistrar.Events());

        // 注册异步事件
        _asyncEventMerger.Add(
            assemblyManifestId,
            eventSystemRegistrar.AsyncEventTypeHandles(),
            eventSystemRegistrar.AsyncEvents());
    }

    // 程序集卸载时移除事件
    public async FTask OnUnload(AssemblyManifest assemblyManifest)
    {
        _eventMerger.Remove(assemblyManifestId);
        _asyncEventMerger.Remove(assemblyManifestId);
    }
}
```

---

## 实际使用场景

### 场景 1：玩家登录流程

```csharp
// 定义登录事件
public struct PlayerLoginEvent
{
    public long PlayerId;
    public string PlayerName;
    public Scene Scene;
}

// 监听器 1：加载玩家数据
public class LoadPlayerDataOnLogin : AsyncEventSystem<PlayerLoginEvent>
{
    protected override async FTask Handler(PlayerLoginEvent self)
    {
        var player = await self.Scene.GetDataBase<Player>().Query(self.PlayerId);
        player.Deserialize(self.Scene);
        Log.Info($"玩家 {self.PlayerName} 数据加载完成");
    }
}

// 监听器 2：发送欢迎消息
public class SendWelcomeMessageOnLogin : EventSystem<PlayerLoginEvent>
{
    protected override void Handler(PlayerLoginEvent self)
    {
        Log.Info($"欢迎回来，{self.PlayerName}！");
        // 发送客户端消息
    }
}

// 监听器 3：记录登录日志
public class LogPlayerLoginOnLogin : AsyncEventSystem<PlayerLoginEvent>
{
    protected override async FTask Handler(PlayerLoginEvent self)
    {
        // 异步记录登录日志到数据库
        await SaveLoginLogToDatabase(self.PlayerId, TimeHelper.Now);
        Log.Info($"玩家 {self.PlayerName} 登录日志已记录");
    }

    private async FTask SaveLoginLogToDatabase(long playerId, long timestamp)
    {
        // 数据库操作
        await FTask.CompletedTask;
    }
}

// 在登录处理器中发布事件
public class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request, G2C_LoginResponse response)
    {
        // 验证账号密码
        var playerId = await ValidateLogin(request.Username, request.Password);

        if (playerId > 0)
        {
            // 发布登录事件，触发所有监听器
            await session.Scene.EventComponent.PublishAsync(new PlayerLoginEvent
            {
                PlayerId = playerId,
                PlayerName = request.Username,
                Scene = session.Scene
            });

            response.Success = true;
        }
        else
        {
            response.Success = false;
            response.ErrorMessage = "账号或密码错误";
        }
    }
}
```

### 场景 2：战斗伤害系统

```csharp
// 定义伤害事件
public struct DamageEvent
{
    public long AttackerId;
    public long DefenderId;
    public int BaseDamage;
    public int FinalDamage;
    public int DamageType;  // 1: 物理, 2: 魔法, 3: 真实
    public bool IsCritical;
    public Scene Scene;
}

// 监听器 1：计算最终伤害
public class CalculateDamage : EventSystem<DamageEvent>
{
    protected override void Handler(DamageEvent self)
    {
        var attacker = self.Scene.GetEntity<Player>(self.AttackerId);
        var defender = self.Scene.GetEntity<Player>(self.DefenderId);

        // 计算防御减免
        var defense = defender.Defense;
        var damageReduction = defense / (defense + 100f);
        self.FinalDamage = (int)(self.BaseDamage * (1 - damageReduction));

        // 暴击计算
        if (Random.Range(0, 100) < attacker.CriticalRate)
        {
            self.IsCritical = true;
            self.FinalDamage = (int)(self.FinalDamage * attacker.CriticalDamage);
        }

        Log.Info($"最终伤害: {self.FinalDamage}，暴击: {self.IsCritical}");
    }
}

// 监听器 2：应用伤害
public class ApplyDamage : EventSystem<DamageEvent>
{
    protected override void Handler(DamageEvent self)
    {
        var defender = self.Scene.GetEntity<Player>(self.DefenderId);
        defender.Health -= self.FinalDamage;

        Log.Info($"玩家 {self.DefenderId} 剩余血量: {defender.Health}");

        // 检查是否死亡
        if (defender.Health <= 0)
        {
            self.Scene.EventComponent.Publish(new PlayerDeathEvent
            {
                PlayerId = self.DefenderId,
                KillerId = self.AttackerId
            });
        }
    }
}

// 监听器 3：显示伤害飘字
public class ShowDamageText : EventSystem<DamageEvent>
{
    protected override void Handler(DamageEvent self)
    {
        var defender = self.Scene.GetEntity<Player>(self.DefenderId);

        // 发送客户端消息显示飘字
        var message = new S2C_ShowDamageText
        {
            PlayerId = self.DefenderId,
            Damage = self.FinalDamage,
            IsCritical = self.IsCritical
        };

        // 发送给附近玩家
        defender.BroadcastToNearby(message);
    }
}

// 监听器 4：触发反击机制
public class TriggerCounterAttack : EventSystem<DamageEvent>
{
    protected override void Handler(DamageEvent self)
    {
        var defender = self.Scene.GetEntity<Player>(self.DefenderId);

        // 检查是否有反击 Buff
        if (defender.HasBuff(BuffType.CounterAttack))
        {
            // 触发反击
            self.Scene.EventComponent.Publish(new DamageEvent
            {
                AttackerId = self.DefenderId,
                DefenderId = self.AttackerId,
                BaseDamage = defender.Attack / 2,
                DamageType = 1,
                Scene = self.Scene
            });
        }
    }
}

// 发起攻击
public void Attack(long attackerId, long defenderId, int baseDamage)
{
    scene.EventComponent.Publish(new DamageEvent
    {
        AttackerId = attackerId,
        DefenderId = defenderId,
        BaseDamage = baseDamage,
        DamageType = 1,
        IsCritical = false,
        Scene = scene
    });
}
```

### 场景 3：成就系统

```csharp
// 定义玩家击杀事件
public struct PlayerKillEvent
{
    public long KillerId;
    public long VictimId;
    public int KillCount;  // 连续击杀数
    public Scene Scene;
}

// 监听器 1：更新击杀统计
public class UpdateKillStatistics : EventSystem<PlayerKillEvent>
{
    protected override void Handler(PlayerKillEvent self)
    {
        var killer = self.Scene.GetEntity<Player>(self.KillerId);
        killer.TotalKills++;
        killer.KillStreak++;

        Log.Info($"玩家 {self.KillerId} 总击杀: {killer.TotalKills}, 连杀: {killer.KillStreak}");
    }
}

// 监听器 2：检查击杀成就
public class CheckKillAchievements : EventSystem<PlayerKillEvent>
{
    protected override void Handler(PlayerKillEvent self)
    {
        var killer = self.Scene.GetEntity<Player>(self.KillerId);

        // 检查总击杀数成就
        if (killer.TotalKills == 100 && !killer.HasAchievement(AchievementId.Kill100))
        {
            self.Scene.EventComponent.Publish(new AchievementUnlockedEvent
            {
                PlayerId = self.KillerId,
                AchievementId = AchievementId.Kill100,
                Scene = self.Scene
            });
        }

        // 检查连杀成就
        if (killer.KillStreak >= 5 && !killer.HasAchievement(AchievementId.KillStreak5))
        {
            self.Scene.EventComponent.Publish(new AchievementUnlockedEvent
            {
                PlayerId = self.KillerId,
                AchievementId = AchievementId.KillStreak5,
                Scene = self.Scene
            });
        }
    }
}

// 监听器 3：广播击杀消息
public class BroadcastKillMessage : EventSystem<PlayerKillEvent>
{
    protected override void Handler(PlayerKillEvent self)
    {
        var killer = self.Scene.GetEntity<Player>(self.KillerId);
        var victim = self.Scene.GetEntity<Player>(self.VictimId);

        // 广播击杀消息给所有玩家
        var message = new S2C_BroadcastKillMessage
        {
            KillerName = killer.Name,
            VictimName = victim.Name,
            KillStreak = killer.KillStreak
        };

        self.Scene.BroadcastToAll(message);
    }
}

// 定义成就解锁事件
public struct AchievementUnlockedEvent
{
    public long PlayerId;
    public int AchievementId;
    public Scene Scene;
}

// 监听器：发放成就奖励
public class GiveAchievementReward : AsyncEventSystem<AchievementUnlockedEvent>
{
    protected override async FTask Handler(AchievementUnlockedEvent self)
    {
        var player = self.Scene.GetEntity<Player>(self.PlayerId);
        player.AddAchievement(self.AchievementId);

        // 异步发放奖励
        await GiveRewardAsync(player, self.AchievementId);

        // 通知客户端
        var message = new S2C_AchievementUnlocked
        {
            AchievementId = self.AchievementId
        };
        player.Send(message);

        Log.Info($"玩家 {self.PlayerId} 解锁成就 {self.AchievementId}");
    }

    private async FTask GiveRewardAsync(Player player, int achievementId)
    {
        // 根据成就 ID 发放奖励
        await FTask.CompletedTask;
    }
}
```

### 场景 4：跨场景事件传递

```csharp
// 定义切换场景事件
public class SceneSwitchEvent : Entity
{
    public long PlayerId { get; set; }
    public int OldSceneId { get; set; }
    public int NewSceneId { get; set; }
    public Scene OldScene { get; set; }
    public Scene NewScene { get; set; }
}

// 监听器 1：保存旧场景数据
public class SaveOldSceneData : AsyncEventSystem<SceneSwitchEvent>
{
    protected override async FTask Handler(SceneSwitchEvent self)
    {
        var player = self.OldScene.GetEntity<Player>(self.PlayerId);

        // 保存玩家在旧场景的数据
        await player.Save();

        // 清理旧场景的临时数据
        player.ClearTemporaryData();

        Log.Info($"玩家 {self.PlayerId} 旧场景 {self.OldSceneId} 数据已保存");
    }
}

// 监听器 2：加载新场景数据
public class LoadNewSceneData : AsyncEventSystem<SceneSwitchEvent>
{
    protected override async FTask Handler(SceneSwitchEvent self)
    {
        // 在新场景中创建玩家实体
        var player = await self.NewScene.GetDataBase<Player>().Query(self.PlayerId);
        player.Deserialize(self.NewScene);

        // 初始化新场景数据
        player.InitializeSceneData(self.NewSceneId);

        Log.Info($"玩家 {self.PlayerId} 新场景 {self.NewSceneId} 数据已加载");
    }
}

// 监听器 3：通知其他玩家
public class NotifyOtherPlayers : EventSystem<SceneSwitchEvent>
{
    protected override void Handler(SceneSwitchEvent self)
    {
        // 通知旧场景玩家：某玩家离开
        var leaveMessage = new S2C_PlayerLeave
        {
            PlayerId = self.PlayerId
        };
        self.OldScene.BroadcastToAll(leaveMessage);

        // 通知新场景玩家：某玩家进入
        var enterMessage = new S2C_PlayerEnter
        {
            PlayerId = self.PlayerId
        };
        self.NewScene.BroadcastToAll(enterMessage);
    }
}

// 执行场景切换
public async FTask SwitchScene(long playerId, int newSceneId)
{
    var oldScene = currentScene;
    var newScene = await GetOrCreateScene(newSceneId);

    // 创建切换事件
    var switchEvent = Entity.Create<SceneSwitchEvent>(oldScene, isPool: true);
    switchEvent.PlayerId = playerId;
    switchEvent.OldSceneId = oldScene.SceneConfigId;
    switchEvent.NewSceneId = newSceneId;
    switchEvent.OldScene = oldScene;
    switchEvent.NewScene = newScene;

    // 发布事件，等待所有异步操作完成
    await oldScene.EventComponent.PublishAsync(switchEvent, isDisposed: true);

    Log.Info($"玩家 {playerId} 场景切换完成");
}
```

---

## 性能优化

### 1. 零装箱调用

Event 系统通过泛型接口实现**零装箱**调用：

```csharp
// 泛型接口定义
public interface IEvent<in T> : IEvent
{
    void Invoke(T self);  // 泛型方法，避免装箱
}

// 内部调用
((IEvent<TEventData>)@event).Invoke(eventData);  // 直接调用泛型方法，无装箱
```

**性能对比：**
```csharp
// ❌ 传统事件系统（有装箱）
public interface IEvent
{
    void Invoke(object eventData);  // 值类型 → object 需要装箱
}

// ✅ Fantasy Event 系统（零装箱）
public interface IEvent<in T>
{
    void Invoke(T eventData);  // 泛型方法，编译器特化，无装箱
}
```

### 2. 使用 Struct 事件减少 GC

对于频繁触发的事件，优先使用 Struct 类型：

```csharp
// ✅ 推荐：Struct 事件，无 GC 分配
public struct PlayerMoveEvent
{
    public long PlayerId;
    public Vector3 Position;
}

scene.EventComponent.Publish(new PlayerMoveEvent
{
    PlayerId = player.Id,
    Position = player.Position
});
// 在栈上分配，无 GC 压力

// ❌ 不推荐：Entity 事件，有 GC 分配（除非需要复杂逻辑）
public class PlayerMoveEvent : Entity
{
    public long PlayerId { get; set; }
    public Vector3 Position { get; set; }
}
```

### 3. Entity 事件的对象池优化

如果必须使用 Entity 事件，利用对象池减少 GC：

```csharp
// 使用对象池创建
var itemDrop = Entity.Create<ItemDropEvent>(scene, isPool: true);
itemDrop.ItemId = 1001;

// 发布后自动回收到对象池
await scene.EventComponent.PublishAsync(itemDrop, isDisposed: true);
```

### 4. 合理使用同步/异步事件

**同步事件（`EventSystem<T>`）：**
- ✅ 适合轻量级、快速执行的逻辑
- ✅ 无异步开销，性能最佳
- ❌ 不适合 IO 操作、网络请求

**异步事件（`AsyncEventSystem<T>`）：**
- ✅ 适合需要等待的操作（数据库、网络、文件 IO）
- ✅ 多个监听器并行执行
- ❌ 有 FTask 分配开销

```csharp
// ✅ 同步事件：UI 更新、简单计算
public class OnPlayerLevelUpUI : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        // 快速执行，无需异步
        UpdateUI(self.NewLevel);
    }
}

// ✅ 异步事件：数据库操作
public class SavePlayerOnLevelUp : AsyncEventSystem<PlayerLevelUpEvent>
{
    protected override async FTask Handler(PlayerLevelUpEvent self)
    {
        // 需要等待数据库完成
        await database.Save(self.PlayerId);
    }
}
```

---

## Event vs SphereEvent 对比

Fantasy Framework 提供两种事件系统：

| 特性 | **Event (EventComponent)** | **SphereEvent (SphereEventComponent)** |
|------|--------------------------|--------------------------------------|
| **作用范围** | **单个 Scene 内**本地事件 | **跨服务器**分布式事件 |
| **通信方式** | 进程内内存调用 | 网络消息传递 |
| **性能** | 极高（纳秒级） | 较低（毫秒级，依赖网络延迟） |
| **使用场景** | 单服逻辑解耦 | 跨服域事件分发 |
| **事件数据** | Struct 或 Entity | 必须实现 ISphereEvent 接口 |
| **监听器接口** | `EventSystem<T>`, `AsyncEventSystem<T>` | `ISphereEvent<T>` |
| **自动注册** | ✅ Source Generator | ✅ Source Generator |
| **错误隔离** | ✅ 单个监听器错误不影响其他 | ✅ 单个订阅者错误不影响其他 |

**使用建议：**
- **本地逻辑解耦**：使用 `EventComponent`（本文档介绍的系统）
- **跨服通知**：使用 `SphereEventComponent`（参见 [09-SphereEvent.md](../../03-Networking/09-SphereEvent.md)）

**示例对比：**
```csharp
// 本地事件：单服内玩家升级
scene.EventComponent.Publish(new PlayerLevelUpEvent
{
    PlayerId = player.Id,
    NewLevel = 10
});

// 跨服事件：通知所有服务器玩家升级
await scene.SphereEventComponent.PublishToRemoteSubscribers(
    new CrossServerPlayerLevelUpEvent
    {
        PlayerId = player.Id,
        NewLevel = 10
    },
    isAutoDisposed: true
);
```

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 事件命名清晰，使用 Event 后缀
public struct PlayerLevelUpEvent { }  // ✅ 好
public struct LevelUp { }  // ❌ 不清晰

// 2. Struct 事件优先，减少 GC
public struct DamageEvent { }  // ✅ 频繁触发用 Struct
public class ComplexQuestEvent : Entity { }  // ✅ 复杂逻辑用 Entity

// 3. 监听器命名体现业务逻辑
public class SavePlayerOnLevelUp : AsyncEventSystem<PlayerLevelUpEvent> { }  // ✅ 好
public class OnPlayerLevelUp : EventSystem<PlayerLevelUpEvent> { }  // ⚠️ 可以，但不如前者清晰

// 4. 合理使用同步/异步
public class UpdateUIOnDamage : EventSystem<DamageEvent> { }  // ✅ UI 更新用同步
public class SaveDamageLog : AsyncEventSystem<DamageEvent> { }  // ✅ 数据库用异步

// 5. Entity 事件使用对象池和自动销毁
var questEvent = Entity.Create<QuestCompleteEvent>(scene, isPool: true);
await scene.EventComponent.PublishAsync(questEvent, isDisposed: true);

// 6. Struct 事件包含必要的 Scene 引用
public struct PlayerLoginEvent
{
    public long PlayerId;
    public Scene Scene;  // ✅ 便于监听器访问 Scene
}

// 7. 多个异步操作并行执行
public class OnPlayerLogin : AsyncEventSystem<PlayerLoginEvent>
{
    protected override async FTask Handler(PlayerLoginEvent self)
    {
        // ✅ 并行执行多个异步操作
        await FTask.WhenAll(
            LoadPlayerData(self.PlayerId),
            LoadFriendList(self.PlayerId),
            LoadMailList(self.PlayerId)
        );
    }
}
```

### ⚠️ 注意事项

```csharp
// 1. 不要在事件监听器中直接抛出异常（已内置捕获，但会记录错误日志）
public class OnPlayerDeath : EventSystem<PlayerDeathEvent>
{
    protected override void Handler(PlayerDeathEvent self)
    {
        throw new Exception("错误！");  // ⚠️ 会被捕获并记录日志，不影响其他监听器
    }
}

// 2. 不要在 Struct 事件中存储大量数据
public struct BadEvent
{
    public int[] LargeArray;  // ❌ 栈空间有限，可能导致栈溢出
}

// 正确做法：使用 Entity 或传递引用
public class GoodEvent : Entity
{
    public List<int> LargeList { get; set; }  // ✅ 堆上分配
}

// 3. Entity 事件必须指定是否自动销毁
scene.EventComponent.Publish(itemDrop);  // ❌ 缺少 isDisposed 参数（默认 true）
scene.EventComponent.Publish(itemDrop, isDisposed: true);  // ✅ 明确指定

// 4. 不要在监听器中发布大量事件（可能导致调用栈过深）
public class BadHandler : EventSystem<Event1>
{
    protected override void Handler(Event1 self)
    {
        // ❌ 递归发布事件
        self.Scene.EventComponent.Publish(new Event2());
    }
}

public class BadHandler2 : EventSystem<Event2>
{
    protected override void Handler(Event2 self)
    {
        // ❌ 又发布回 Event1
        self.Scene.EventComponent.Publish(new Event1());
    }
}

// 5. 异步事件不要忘记 await
await scene.EventComponent.PublishAsync(loginEvent);  // ✅ 等待完成
scene.EventComponent.PublishAsync(loginEvent);  // ❌ 忘记 await，可能导致未完成就继续执行
```

---

## 常见问题

### Q1: Event 系统和 C# 原生 event 有什么区别？

**A:** Fantasy Event 系统是基于**发布-订阅模式**的事件系统，与 C# 原生 `event` 有本质区别：

| 特性 | **Fantasy Event** | **C# event** |
|------|------------------|--------------|
| **订阅方式** | 自动注册（Source Generator） | 手动订阅 (`+=`) |
| **解耦性** | 完全解耦（发布者不知道订阅者） | 需要持有发布者引用 |
| **性能** | 零装箱，编译时优化 | 委托调用，有装箱（值类型） |
| **异步支持** | 原生支持 `FTask` | 需要手动处理 `Task` |
| **错误隔离** | 单个监听器错误不影响其他 | 异常会中断后续委托 |
| **热重载** | ✅ 支持 | ❌ 不支持 |

### Q2: 如何在监听器中访问其他组件？

**A:** 通过 `Scene` 引用访问：

```csharp
public struct PlayerLevelUpEvent
{
    public long PlayerId;
    public Scene Scene;  // 传递 Scene 引用
}

public class OnPlayerLevelUp : EventSystem<PlayerLevelUpEvent>
{
    protected override void Handler(PlayerLevelUpEvent self)
    {
        // 通过 Scene 获取实体
        var player = self.Scene.GetEntity<Player>(self.PlayerId);

        // 访问其他组件
        var inventory = player.GetComponent<InventoryComponent>();
        inventory.AddItem(1001, 10);

        // 访问 Scene 级别组件
        var rankingComponent = self.Scene.GetComponent<RankingComponent>();
        rankingComponent.UpdateRanking(player);
    }
}
```

### Q3: 同一事件有多个监听器时，执行顺序是什么？

**A:** 执行顺序由 **Source Generator 生成的注册顺序**决定，通常按以下规则：
1. **同步事件**：按类名字母顺序依次执行
2. **异步事件**：并行执行（通过 `FTask.WaitAll()`）

如果需要严格的执行顺序，建议：
- 使用**事件链**（一个事件触发另一个事件）
- 或在单个监听器中按顺序调用多个方法

```csharp
// 方案 1：事件链
public class Step1Handler : EventSystem<Event1>
{
    protected override void Handler(Event1 self)
    {
        // 步骤 1
        self.Scene.EventComponent.Publish(new Event2());  // 触发下一步
    }
}

// 方案 2：单个监听器按顺序执行
public class OrderedHandler : AsyncEventSystem<PlayerLoginEvent>
{
    protected override async FTask Handler(PlayerLoginEvent self)
    {
        await Step1(self);
        await Step2(self);
        await Step3(self);
    }
}
```

### Q4: Entity 事件何时被销毁？

**A:** 根据 `isDisposed` 参数决定：

```csharp
// 自动销毁（默认行为）
scene.EventComponent.Publish(itemDrop, isDisposed: true);
// 所有监听器处理完成后，itemDrop.Dispose() 被调用

// 手动管理生命周期
scene.EventComponent.Publish(itemDrop, isDisposed: false);
// 监听器处理完成后，itemDrop 仍然存在，需要手动调用 Dispose()
```

**最佳实践：**
- ✅ 大多数情况使用 `isDisposed: true`（自动管理）
- ⚠️ 只有在需要延长生命周期时使用 `isDisposed: false`

### Q5: 如何调试事件监听器？

**A:** 使用以下方法：

```csharp
// 方法 1：在监听器中打印日志
public class OnPlayerLogin : EventSystem<PlayerLoginEvent>
{
    protected override void Handler(PlayerLoginEvent self)
    {
        Log.Debug($"[OnPlayerLogin] 玩家 {self.PlayerId} 登录");
        // 业务逻辑
    }
}

// 方法 2：在发布事件前后打印日志
Log.Debug("开始发布 PlayerLoginEvent");
scene.EventComponent.Publish(loginEvent);
Log.Debug("PlayerLoginEvent 发布完成");

// 方法 3：检查是否有监听器注册
// 查看生成的 EventSystemRegistrar.g.cs 文件
// 位置: obj/Debug/net9.0/generated/...
```

### Q6: Struct 事件可以修改自身字段吗？

**A:** 可以，但**不推荐**。由于 Struct 是值类型，在监听器中的修改不会影响调用者：

```csharp
public struct TestEvent
{
    public int Value;
}

public class Handler1 : EventSystem<TestEvent>
{
    protected override void Handler(TestEvent self)
    {
        self.Value = 100;  // ⚠️ 修改的是副本，不影响其他监听器
    }
}

public class Handler2 : EventSystem<TestEvent>
{
    protected override void Handler(TestEvent self)
    {
        Log.Info($"Value: {self.Value}");  // 输出原始值，不是 100
    }
}

// 发布事件
scene.EventComponent.Publish(new TestEvent { Value = 1 });
```

**解决方案：**
- 使用 **Entity 事件**（引用类型，所有监听器共享同一实例）
- 或通过 **Scene 引用**访问共享数据

---

## 总结

Event 系统是 Fantasy Framework 的**核心解耦机制**，提供了：

- **类型安全**: 编译时类型检查，避免运行时错误
- **高性能**: 零装箱调用，编译时优化
- **自动注册**: Source Generator 自动生成注册代码
- **灵活性**: 支持同步/异步、Struct/Entity 事件
- **热重载**: 支持程序集热更新
- **错误隔离**: 单个监听器错误不影响其他监听器

**设计理念：**
通过事件系统实现模块解耦，提升代码可维护性和扩展性，同时保持极致性能。

---

## 相关文档

- [01-ECS.md](01-ECS.md) - Entity-Component-System 详解
- [02-ISupportedMultiEntity.md](02-ISupportedMultiEntity.md) - 多实例组件详解
- [03-Scene.md](03-Scene.md) - Scene 和 SubScene 使用
- [09-SphereEvent.md](../../03-Networking/09-SphereEvent.md) - 跨服域事件系统（规划中）
