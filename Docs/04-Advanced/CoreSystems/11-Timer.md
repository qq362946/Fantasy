# Timer 系统使用指南

## 概述

`TimerComponent` 是 Fantasy Framework 的定时任务调度组件,提供了**高性能、易用**的定时器功能。支持一次性定时器、重复定时器和异步等待,适用于游戏逻辑中的延时执行、周期任务等场景。

**Timer 系统的主要功能:**
- 异步等待指定时间 (WaitAsync / WaitTillAsync)
- 一次性定时器 (OnceTimer / OnceTillTimer)
- 重复执行定时器 (RepeatedTimer)
- 帧定时器 (FrameTimer)
- 支持取消令牌 (FCancellationToken)
- 集成事件系统

**源码位置:**
- TimerComponent: `/Fantasy.Packages/Fantasy.Net/Runtime/Core/Entitas/Component/TimerComponent/`
- FTask 简化方法: `/Fantasy.Packages/Fantasy.Net/Runtime/Core/FTask/FTask.Extension/FTask.Tools.cs`

💡 **推荐使用方式:**
框架提供了 `FTask` 静态方法作为 TimerComponent 的简化封装,推荐优先使用:
```csharp
// ✅ 推荐: 使用 FTask 简化方法
await FTask.Wait(scene, 1000);

// 也可以: 使用 TimerComponent 方法
await scene.TimerComponent.Net.WaitAsync(1000);
```

---

## 核心概念

### 1. TimerComponent 结构

TimerComponent 包含两个定时器调度器:

```csharp
public sealed class TimerComponent : Entity
{
    // 使用系统时间的调度器 (.NET Server)
    public TimerSchedulerNet Net { get; private set; }

    // 使用 Unity 时间的调度器 (Unity Client)
#if FANTASY_UNITY
    public TimerSchedulerNetUnity Unity { get; private set; }
#endif
}
```

**调度器选择:**
- **服务器端**: 使用 `scene.TimerComponent.Net`
- **Unity 客户端**: 使用 `scene.TimerComponent.Unity`

### 2. 定时器类型

框架提供三种定时器类型 (`TimerType` 枚举):

| 类型 | 说明 | 适用场景 |
|------|------|---------|
| **OnceWaitTimer** | 异步等待定时器 | `WaitAsync()` / `WaitTillAsync()` |
| **OnceTimer** | 一次性定时器 | 延迟执行一次任务 |
| **RepeatedTimer** | 重复定时器 | 周期性执行任务 |

### 3. 驱动机制

Timer 系统需要在主循环中调用 `Update()` 方法驱动:

```csharp
// 框架自动在 TimerComponentUpdateSystem 中调用
public sealed class TimerComponentUpdateSystem : UpdateSystem<TimerComponent>
{
    protected override void Update(TimerComponent self)
    {
        self.Update(); // 驱动 Net 和 Unity 调度器
    }
}
```

⚠️ **注意:** 只有定期调用 `Update()`,定时器才会正常运转。框架已自动注册 UpdateSystem,无需手动调用。

---

## 基础使用

### 1. 获取 TimerComponent

TimerComponent 是 Scene 的核心组件,在 Scene 创建时自动初始化:

```csharp
// 服务器端
var scene = await Scene.Create(SceneRuntimeMode.MainThread);
var timerNet = scene.TimerComponent.Net;

// Unity 客户端
#if FANTASY_UNITY
var timerUnity = scene.TimerComponent.Unity;
#endif
```

### 2. 异步等待 - WaitAsync()

异步等待是最常用的定时器功能,用于在协程中等待指定时间:

```csharp
using Fantasy.Async;
using Fantasy.Timer;

public class PlayerBuff
{
    public async FTask ApplyBuff(Scene scene, long duration)
    {
        Log.Info("Buff 生效");

        // 方式 1: 使用 TimerComponent
        await scene.TimerComponent.Net.WaitAsync(duration);

        // 方式 2: 使用 FTask 简化方法 (推荐)
        await FTask.Wait(scene, duration);

        Log.Info("Buff 过期");
    }
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public async FTask<bool> WaitAsync(long time, FCancellationToken cancellationToken = null)

// FTask 简化方法 (推荐)
public static FTask<bool> Wait(Scene scene, long time, FCancellationToken cancellationToken = null)
```

**参数说明:**
- `time`: 等待的时间长度 (毫秒)
- `cancellationToken`: 可选的取消令牌
- **返回值**: `true` 表示正常完成, `false` 表示被取消

💡 **提示:** `FTask.Wait()` 是 `scene.TimerComponent.Net.WaitAsync()` 的简化方法,推荐使用。

**支持取消令牌:**
```csharp
public async FTask DelayedTask(Scene scene)
{
    var cts = new FCancellationToken();

    // 3 秒后取消等待
    FTask.OnceTimer(scene, 3000, () => cts.Cancel());

    // 等待 10 秒,但可能被提前取消 (使用简化方法)
    bool completed = await FTask.Wait(scene, 10000, cts);

    if (completed)
    {
        Log.Info("等待完成");
    }
    else
    {
        Log.Info("等待被取消");
    }
}
```

### 3. 等待到指定时间 - WaitTillAsync()

等待直到某个具体的时间戳:

```csharp
public async FTask WaitUntilMidnight(Scene scene)
{
    // 计算今天午夜的时间戳
    var midnight = GetTodayMidnightTimestamp();

    Log.Info($"等待到午夜: {midnight}");

    // 方式 1: 使用 TimerComponent
    await scene.TimerComponent.Net.WaitTillAsync(midnight);

    // 方式 2: 使用 FTask 简化方法 (推荐)
    await FTask.WaitTill(scene, midnight);

    Log.Info("午夜到了,执行每日重置");
    ResetDailyData();
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public async FTask<bool> WaitTillAsync(long tillTime, FCancellationToken cancellationToken = null)

// FTask 简化方法 (推荐)
public static FTask<bool> WaitTill(Scene scene, long tillTime, FCancellationToken cancellationToken = null)
```

**参数说明:**
- `tillTime`: 等待的目标时间戳 (毫秒)
- `cancellationToken`: 可选的取消令牌

⚠️ **注意:** 如果 `tillTime` 小于当前时间,会立即返回 `true`。

💡 **提示:** `FTask.WaitTill()` 是 `scene.TimerComponent.Net.WaitTillAsync()` 的简化方法,推荐使用。

### 4. 等待一帧 - WaitFrameAsync()

等待一帧时间 (取决于 Update 调用频率):

```csharp
public async FTask ProcessInBatches(Scene scene, List<Player> players)
{
    foreach (var player in players)
    {
        ProcessPlayer(player);

        // 方式 1: 使用 TimerComponent
        await scene.TimerComponent.Net.WaitFrameAsync();

        // 方式 2: 使用 FTask 简化方法 (推荐)
        await FTask.WaitFrame(scene);
    }

    Log.Info("批处理完成");
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public async FTask WaitFrameAsync()

// FTask 简化方法 (推荐)
public static FTask WaitFrame(Scene scene)
```

⚠️ **注意:**
- **服务器端**: 等待时间取决于 UpdateSystem 的调用频率
- **Unity 客户端**: 等待一个 Unity 渲染帧的时间

💡 **提示:** `FTask.WaitFrame()` 是 `scene.TimerComponent.Net.WaitFrameAsync()` 的简化方法,推荐使用。

### 5. 一次性定时器 - OnceTimer()

延迟执行一次回调,适用于"N 秒后执行某操作":

```csharp
public void StartCountdown(Scene scene)
{
    Log.Info("倒计时开始");

    // 方式 1: 使用 TimerComponent
    long timerId = scene.TimerComponent.Net.OnceTimer(5000, () =>
    {
        Log.Info("倒计时结束!");
        StartBattle();
    });

    // 方式 2: 使用 FTask 简化方法 (推荐)
    long timerId2 = FTask.OnceTimer(scene, 5000, () =>
    {
        Log.Info("倒计时结束!");
        StartBattle();
    });

    // 可以保存 timerId 用于取消
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public long OnceTimer(long time, Action action)

// FTask 简化方法 (推荐)
public static long OnceTimer(Scene scene, long time, Action action)
```

**参数说明:**
- `time`: 延迟时间 (毫秒)
- `action`: 定时器触发时执行的回调
- **返回值**: 定时器 ID,可用于取消

💡 **提示:** `FTask.OnceTimer()` 是 `scene.TimerComponent.Net.OnceTimer()` 的简化方法,推荐使用。

### 6. 到指定时间的一次性定时器 - OnceTillTimer()

在指定时间戳执行回调:

```csharp
public void ScheduleAtSpecificTime(Scene scene)
{
    // 计算 10 分钟后的时间戳
    long tenMinutesLater = TimeHelper.Now + 600000;

    // 方式 1: 使用 TimerComponent
    long timerId = scene.TimerComponent.Net.OnceTillTimer(tenMinutesLater, () =>
    {
        Log.Info("10 分钟到了");
        RefreshShop();
    });

    // 方式 2: 使用 FTask 简化方法 (推荐)
    long timerId2 = FTask.OnceTillTimer(scene, tenMinutesLater, () =>
    {
        Log.Info("10 分钟到了");
        RefreshShop();
    });
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public long OnceTillTimer(long tillTime, Action action)

// FTask 简化方法 (推荐)
public static long OnceTillTimer(Scene scene, long tillTime, Action action)
```

⚠️ **注意:** 如果 `tillTime` 小于当前时间,会记录错误日志,但仍会立即执行回调。

💡 **提示:** `FTask.OnceTillTimer()` 是 `scene.TimerComponent.Net.OnceTillTimer()` 的简化方法,推荐使用。

### 7. 重复定时器 - RepeatedTimer()

周期性重复执行回调:

```csharp
public class MonsterSpawner
{
    private long _spawnTimerId;

    public void StartSpawning(Scene scene)
    {
        Log.Info("开始刷怪");

        // 方式 1: 使用 TimerComponent
        _spawnTimerId = scene.TimerComponent.Net.RepeatedTimer(30000, () =>
        {
            SpawnMonster();
            Log.Info("刷新了一只怪物");
        });

        // 方式 2: 使用 FTask 简化方法 (推荐)
        _spawnTimerId = FTask.RepeatedTimer(scene, 30000, () =>
        {
            SpawnMonster();
            Log.Info("刷新了一只怪物");
        });
    }

    public void StopSpawning(Scene scene)
    {
        // 停止刷怪 (两种方式都可以)
        FTask.RemoveTimer(scene, ref _spawnTimerId);
        Log.Info("停止刷怪");
    }

    private void SpawnMonster()
    {
        // 刷怪逻辑
    }
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public long RepeatedTimer(long time, Action action)

// FTask 简化方法 (推荐)
public static long RepeatedTimer(Scene scene, long time, Action action)
```

**参数说明:**
- `time`: 重复间隔时间 (毫秒)
- `action`: 每次触发时执行的回调
- **返回值**: 定时器 ID,可用于取消

⚠️ **注意:**
- 重复定时器会无限执行,直到调用 `Remove()` 取消
- 时间间隔不能小于 0,否则会记录错误日志并返回 0

💡 **提示:** `FTask.RepeatedTimer()` 是 `scene.TimerComponent.Net.RepeatedTimer()` 的简化方法,推荐使用。

### 8. 帧定时器 - FrameTimer()

每帧重复执行回调:

```csharp
public class CombatSystem
{
    private long _updateTimerId;

    public void StartCombat(Scene scene)
    {
        // 每帧更新战斗逻辑
        _updateTimerId = scene.TimerComponent.Net.FrameTimer(() =>
        {
            UpdateCombat();
        });
    }

    public void StopCombat(Scene scene)
    {
        scene.TimerComponent.Net.Remove(_updateTimerId);
    }

    private void UpdateCombat()
    {
        // 战斗逻辑更新
    }
}
```

⚠️ **注意:**
- **服务器端**: 重复间隔取决于 UpdateSystem 的调用频率
- **Unity 客户端**: 每个渲染帧执行一次

### 9. 取消定时器 - Remove()

取消正在运行的定时器:

```csharp
public class BossRaid
{
    private long _enrageTimerId;

    public void StartBattle(Scene scene)
    {
        // 10 分钟后 Boss 狂暴
        _enrageTimerId = FTask.OnceTimer(scene, 600000, () =>
        {
            BossEnrage();
        });
    }

    public void BossDefeated(Scene scene)
    {
        // 方式 1: 使用 TimerComponent (取消并重置 ID)
        bool removed = scene.TimerComponent.Net.Remove(ref _enrageTimerId);

        // 方式 2: 使用 FTask 简化方法 (推荐)
        bool removed2 = FTask.RemoveTimer(scene, ref _enrageTimerId);

        if (removed)
        {
            Log.Info("取消了 Boss 狂暴定时器");
        }
    }

    private void BossEnrage()
    {
        Log.Info("Boss 进入狂暴状态!");
    }
}
```

**方法签名:**
```csharp
// TimerComponent 方法
public bool Remove(ref long timerId)  // 取消并重置 ID 为 0
public bool Remove(long timerId)      // 只取消,不修改 ID

// FTask 简化方法 (推荐)
public static bool RemoveTimer(Scene scene, ref long timerId)
```

**返回值:**
- `true`: 成功取消
- `false`: 定时器不存在 (可能已经执行或已被取消)

💡 **提示:** `FTask.RemoveTimer()` 是 `scene.TimerComponent.Net.Remove()` 的简化方法,推荐使用。

---

## Unity 客户端 Timer 方法

在 Unity 客户端中,框架提供了专门的 `Unity` 前缀方法,使用 Unity 的 Time 时间系统:

```csharp
#if FANTASY_UNITY
// Unity 客户端异步等待
await FTask.UnityWait(scene, 1000);

// Unity 客户端等到指定时间
await FTask.UnityWaitTill(scene, targetTime);

// Unity 客户端等待一帧
await FTask.UnityWaitFrame(scene);

// Unity 客户端一次性定时器
long timerId = FTask.UnityOnceTimer(scene, 5000, Callback);

// Unity 客户端到指定时间的定时器
long timerId = FTask.UnityOnceTillTimer(scene, targetTime, Callback);

// Unity 客户端重复定时器
long timerId = FTask.UnityRepeatedTimer(scene, 1000, Callback);

// Unity 客户端取消定时器
FTask.UnityRemoveTimer(scene, ref timerId);
#endif
```

**Unity vs Net 区别:**
- **Net 方法**: 使用系统时间 (`TimeHelper.Now`),适用于服务器端
- **Unity 方法**: 使用 Unity Time 系统,受 `Time.timeScale` 影响,适用于 Unity 客户端

**使用场景:**
```csharp
#if FANTASY_UNITY
public class UnityTimerExample : MonoBehaviour
{
    private Scene _scene;

    async void Start()
    {
        _scene = await Fantasy.Scene.Create(SceneRuntimeMode.MainThread);

        // 游戏逻辑定时器 (受 Time.timeScale 影响)
        await FTask.UnityWait(_scene, 3000);
        Log.Info("3 秒后执行 (游戏时间)");

        // 实时定时器 (不受 Time.timeScale 影响)
        // 需要使用 Net 方法
        await FTask.Wait(_scene, 3000);
        Log.Info("3 秒后执行 (真实时间)");
    }
}
#endif
```

---

## 与事件系统集成

Timer 系统可以与 Event 系统结合,通过定时器触发事件。

✨ **使用事件方式的优点:**
- ✅ **支持热重载**: 事件监听器会随程序集重载自动更新
- ✅ **解耦性更强**: 定时器不直接依赖具体的业务逻辑
- ✅ **易于扩展**: 可以添加多个监听器处理同一定时事件
- ❌ **Action 方式不支持热重载**: 使用 `Action` 回调的定时器在热重载后仍执行旧代码

**对比示例:**
```csharp
// ❌ 不支持热重载: 使用 Action 回调
FTask.OnceTimer(scene, 5000, () =>
{
    Log.Info("这段代码不会随热重载更新");
    // 即使修改了这里的代码并热重载,定时器仍会执行旧代码
});

// ✅ 支持热重载: 使用事件触发
public struct RefreshShopEvent { }

public class OnRefreshShop : EventSystem<RefreshShopEvent>
{
    protected override void Handler(RefreshShopEvent self)
    {
        Log.Info("这段代码会随热重载更新");
        // 热重载后,定时器会执行新的代码逻辑
    }
}

FTask.OnceTimer(scene, 5000, new RefreshShopEvent());
```

💡 **推荐做法:**
- 对于需要热重载的游戏逻辑,使用事件方式的定时器
- 对于框架级别的系统逻辑,可以使用 Action 方式

---

### 1. 定时触发事件 (一次性)

```csharp
// 定义事件
public struct BattleStartEvent
{
    public int BattleId;
    public Scene Scene;
}

// 创建事件监听器
public class OnBattleStart : EventSystem<BattleStartEvent>
{
    protected override void Handler(BattleStartEvent self)
    {
        Log.Info($"战斗 {self.BattleId} 开始!");
        // 战斗开始逻辑
    }
}

// 使用定时器触发事件
public void ScheduleBattle(Scene scene, int battleId)
{
    Log.Info("5 秒后开始战斗");

    // 5 秒后触发事件
    scene.TimerComponent.Net.OnceTimer(5000, new BattleStartEvent
    {
        BattleId = battleId,
        Scene = scene
    });
}
```

### 2. 定时触发事件 (重复)

```csharp
// 定义心跳事件
public struct ServerHeartbeatEvent
{
    public long Timestamp;
}

// 创建事件监听器
public class OnServerHeartbeat : EventSystem<ServerHeartbeatEvent>
{
    protected override void Handler(ServerHeartbeatEvent self)
    {
        Log.Info($"服务器心跳: {self.Timestamp}");
        // 心跳逻辑 (如统计在线人数、检查服务器状态)
    }
}

// 使用重复定时器触发心跳事件
public void StartServerHeartbeat(Scene scene)
{
    // 每 60 秒触发一次心跳事件
    scene.TimerComponent.Net.RepeatedTimer(60000, new ServerHeartbeatEvent
    {
        Timestamp = TimeHelper.Now
    });
}
```

**事件定时器方法签名:**
```csharp
// 一次性事件定时器
public long OnceTimer<T>(long time, T timerHandlerType) where T : struct

// 指定时间触发事件
public long OnceTillTimer<T>(long tillTime, T timerHandlerType) where T : struct

// 重复事件定时器
public long RepeatedTimer<T>(long time, T timerHandlerType) where T : struct
```

---

## 实际使用场景

### 场景 1: 技能冷却系统

```csharp
public class SkillCooldownComponent : Entity
{
    private readonly Dictionary<int, long> _cooldownTimers = new();

    // 使用技能
    public bool UseSkill(int skillId, long cooldownTime)
    {
        // 检查是否在冷却中
        if (_cooldownTimers.ContainsKey(skillId))
        {
            Log.Info($"技能 {skillId} 冷却中");
            return false;
        }

        Log.Info($"使用技能 {skillId}");

        // 执行技能逻辑
        ExecuteSkill(skillId);

        // 启动冷却定时器
        long timerId = Scene.TimerComponent.Net.OnceTimer(cooldownTime, () =>
        {
            // 冷却结束,移除记录
            _cooldownTimers.Remove(skillId);
            Log.Info($"技能 {skillId} 冷却完成");
        });

        _cooldownTimers[skillId] = timerId;
        return true;
    }

    // 清理所有冷却定时器
    public void ClearAllCooldowns()
    {
        foreach (var timerId in _cooldownTimers.Values)
        {
            Scene.TimerComponent.Net.Remove(timerId);
        }
        _cooldownTimers.Clear();
    }

    private void ExecuteSkill(int skillId)
    {
        // 技能执行逻辑
    }
}
```

### 场景 2: Buff/Debuff 系统

```csharp
public class BuffComponent : Entity
{
    private readonly Dictionary<int, BuffData> _activeBuffs = new();

    private class BuffData
    {
        public int BuffId;
        public long ExpireTimerId;
        public long TickTimerId;
    }

    // 添加 Buff
    public void AddBuff(int buffId, long duration, long tickInterval)
    {
        // 移除旧 Buff
        RemoveBuff(buffId);

        var buffData = new BuffData { BuffId = buffId };

        // 启动 Buff 过期定时器
        buffData.ExpireTimerId = Scene.TimerComponent.Net.OnceTimer(duration, () =>
        {
            OnBuffExpire(buffId);
        });

        // 启动 Buff 持续效果定时器 (如持续回血)
        buffData.TickTimerId = Scene.TimerComponent.Net.RepeatedTimer(tickInterval, () =>
        {
            OnBuffTick(buffId);
        });

        _activeBuffs[buffId] = buffData;
        Log.Info($"添加 Buff {buffId}, 持续 {duration}ms");
    }

    // 移除 Buff
    public void RemoveBuff(int buffId)
    {
        if (!_activeBuffs.TryGetValue(buffId, out var buffData))
        {
            return;
        }

        // 取消定时器
        Scene.TimerComponent.Net.Remove(buffData.ExpireTimerId);
        Scene.TimerComponent.Net.Remove(buffData.TickTimerId);

        _activeBuffs.Remove(buffId);
        Log.Info($"移除 Buff {buffId}");
    }

    private void OnBuffExpire(int buffId)
    {
        Log.Info($"Buff {buffId} 过期");
        RemoveBuff(buffId);
    }

    private void OnBuffTick(int buffId)
    {
        Log.Info($"Buff {buffId} 触发持续效果");
        // 持续效果逻辑 (如每秒回血)
    }
}
```

### 场景 3: 每日重置系统

```csharp
public class DailyResetSystem
{
    private long _resetTimerId;

    public void Initialize(Scene scene)
    {
        // 计算下次凌晨 0 点的时间戳
        long nextMidnight = CalculateNextMidnight();

        // 在凌晨 0 点触发重置
        _resetTimerId = scene.TimerComponent.Net.OnceTillTimer(nextMidnight, () =>
        {
            OnDailyReset(scene);
        });

        Log.Info($"每日重置定时器已启动, 下次重置: {nextMidnight}");
    }

    private async void OnDailyReset(Scene scene)
    {
        Log.Info("执行每日重置");

        // 重置所有玩家的每日数据
        await ResetAllPlayerDailyData(scene);

        // 重置商店
        ResetShop();

        // 重置副本次数
        ResetDungeonCounts();

        // 设置下一次重置定时器
        Initialize(scene);
    }

    private long CalculateNextMidnight()
    {
        var now = DateTime.Now;
        var tomorrow = now.Date.AddDays(1);
        return new DateTimeOffset(tomorrow).ToUnixTimeMilliseconds();
    }

    private async FTask ResetAllPlayerDailyData(Scene scene)
    {
        // 重置逻辑
        await FTask.CompletedTask;
    }

    private void ResetShop() { }
    private void ResetDungeonCounts() { }
}
```

### 场景 4: 战斗倒计时

```csharp
public class BattleCountdown
{
    private long _countdownTimerId;

    public void StartCountdown(Scene scene, int seconds)
    {
        int remainingSeconds = seconds;

        Log.Info($"战斗倒计时开始: {remainingSeconds} 秒");

        // 每秒更新倒计时
        _countdownTimerId = scene.TimerComponent.Net.RepeatedTimer(1000, () =>
        {
            remainingSeconds--;

            if (remainingSeconds > 0)
            {
                Log.Info($"倒计时: {remainingSeconds} 秒");
                BroadcastCountdown(scene, remainingSeconds);
            }
            else
            {
                Log.Info("倒计时结束, 战斗开始!");
                scene.TimerComponent.Net.Remove(_countdownTimerId);
                StartBattle(scene);
            }
        });
    }

    private void BroadcastCountdown(Scene scene, int seconds)
    {
        // 广播倒计时给所有玩家
    }

    private void StartBattle(Scene scene)
    {
        // 开始战斗
    }
}
```

### 场景 5: 延时保存数据

```csharp
public class PlayerDataSaver
{
    private long _saveTimerId;
    private bool _dataDirty;

    // 标记数据已修改
    public void MarkDirty(Scene scene)
    {
        _dataDirty = true;

        // 取消之前的保存定时器
        if (_saveTimerId != 0)
        {
            scene.TimerComponent.Net.Remove(_saveTimerId);
        }

        // 5 秒后自动保存
        _saveTimerId = scene.TimerComponent.Net.OnceTimer(5000, () =>
        {
            SaveData(scene);
        });
    }

    private async void SaveData(Scene scene)
    {
        if (!_dataDirty)
        {
            return;
        }

        Log.Info("保存玩家数据");

        // 执行保存逻辑
        await scene.GetDataBase<Player>().Save(GetPlayerData());

        _dataDirty = false;
        _saveTimerId = 0;
    }

    private Player GetPlayerData()
    {
        // 获取玩家数据
        return null;
    }
}
```

---

## 性能优化

### 1. 合理使用定时器类型

```csharp
// ✅ 推荐: 需要等待时使用 FTask.Wait
public async FTask LoadResourcesAsync(Scene scene)
{
    Log.Info("开始加载资源");
    await FTask.Wait(scene, 1000);
    Log.Info("资源加载完成");
}

// ❌ 不推荐: 使用 OnceTimer + 回调 (增加闭包开销)
public void LoadResources(Scene scene)
{
    Log.Info("开始加载资源");
    FTask.OnceTimer(scene, 1000, () =>
    {
        Log.Info("资源加载完成");
    });
}
```

### 2. 及时取消不需要的定时器

```csharp
public class EnemyAI
{
    private long _aiUpdateTimerId;

    public void StartAI(Scene scene)
    {
        _aiUpdateTimerId = FTask.RepeatedTimer(scene, 100, UpdateAI);
    }

    public void StopAI(Scene scene)
    {
        // ✅ 及时取消定时器,避免内存泄漏
        FTask.RemoveTimer(scene, ref _aiUpdateTimerId);
    }

    private void UpdateAI()
    {
        // AI 更新逻辑
    }
}
```

### 3. 避免创建大量短周期定时器

```csharp
// ❌ 不推荐: 为每个玩家创建独立的 100ms 定时器
public void BadExample(Scene scene, List<Player> players)
{
    foreach (var player in players)
    {
        FTask.RepeatedTimer(scene, 100, () =>
        {
            UpdatePlayer(player);
        });
    }
}

// ✅ 推荐: 使用一个定时器处理所有玩家
public void GoodExample(Scene scene, List<Player> players)
{
    FTask.RepeatedTimer(scene, 100, () =>
    {
        foreach (var player in players)
        {
            UpdatePlayer(player);
        }
    });
}
```

### 4. 使用 ref 参数自动重置 ID

```csharp
public class TimerManager
{
    private long _timerId;

    public void StartTimer(Scene scene)
    {
        _timerId = FTask.OnceTimer(scene, 5000, Callback);
    }

    public void CancelTimer(Scene scene)
    {
        // ✅ 使用 ref 参数, RemoveTimer 后自动将 _timerId 置为 0
        FTask.RemoveTimer(scene, ref _timerId);

        // 无需手动 _timerId = 0;
    }

    private void Callback() { }
}
```

---

## 常见问题

### Q1: 定时器回调中抛出异常会怎样?

**A:** 异常会被捕获并记录错误日志,但**不会影响其他定时器**的执行。框架内部有异常保护机制。

```csharp
scene.TimerComponent.Net.OnceTimer(1000, () =>
{
    throw new Exception("定时器错误");
    // 会记录错误日志: timerAction {...}
});

// 其他定时器不受影响
scene.TimerComponent.Net.OnceTimer(2000, () =>
{
    Log.Info("这个定时器正常执行");
});
```

### Q2: WaitAsync 和 Task.Delay 有什么区别?

**A:** `WaitAsync` 是基于框架的 `FTask` 和 Timer 系统,性能更高且与框架生命周期集成:

| 特性 | **WaitAsync** | **Task.Delay** |
|------|---------------|----------------|
| **性能** | 高 (对象池复用) | 较低 (GC 压力) |
| **取消支持** | `FCancellationToken` | `CancellationToken` |
| **框架集成** | ✅ 与 Scene 生命周期绑定 | ❌ 独立的 Task 系统 |
| **时间精度** | 取决于 Update 频率 | 系统线程调度 |

### Q3: 定时器的时间精度是多少?

**A:** 定时器精度取决于 `Update()` 的调用频率:
- **服务器端**: 取决于 TimerComponentUpdateSystem 的执行频率
- **Unity 客户端**: 每帧调用一次 (取决于实际帧率)

**示例:**
```csharp
// 假设 Update 每 100ms 调用一次
scene.TimerComponent.Net.OnceTimer(150, Callback);
// 实际触发时间: 200ms (下一次 Update 时,会有误差)
```

⚠️ **注意:** 定时器触发时间会有误差,误差范围为一次 Update 的时间间隔。

### Q4: 重复定时器会累积误差吗?

**A:** **不会**。每次触发后会重新计算下次触发时间:

```csharp
// 定时器实现 (简化版)
timerAction.StartTime = Now();  // 更新起始时间
AddTimer(ref timerAction);       // 重新调度
action();                        // 执行回调
```

### Q5: 可以在定时器回调中创建新的定时器吗?

**A:** **可以**,框架支持嵌套定时器:

```csharp
scene.TimerComponent.Net.OnceTimer(1000, () =>
{
    Log.Info("第一个定时器触发");

    // ✅ 可以在回调中创建新定时器
    scene.TimerComponent.Net.OnceTimer(2000, () =>
    {
        Log.Info("嵌套定时器触发");
    });
});
```

### Q6: Scene 销毁后,定时器会自动取消吗?

**A:** **会**。TimerComponent 是 Entity 的子类,当 Scene 销毁时,TimerComponent 也会被销毁,所有定时器自动清理。

```csharp
var scene = await Scene.Create(SceneRuntimeMode.MainThread);
scene.TimerComponent.Net.RepeatedTimer(1000, () =>
{
    Log.Info("重复定时器");
});

// Scene 销毁时,定时器自动清理
await scene.Dispose();
```

### Q7: 为什么事件方式的定时器支持热重载,而 Action 方式不支持?

**A:** 这是由于 **闭包捕获** 和 **事件系统注册机制** 的区别:

**Action 方式 (不支持热重载):**
```csharp
// 创建定时器时,Lambda 表达式被编译成闭包
FTask.OnceTimer(scene, 5000, () =>
{
    RefreshShop();  // 这个方法引用在创建时就被捕获了
});

// 热重载后:
// - 定时器仍然持有旧的闭包引用
// - 执行的是旧程序集中的 RefreshShop() 方法
```

**事件方式 (支持热重载):**
```csharp
// 定义事件
public struct RefreshShopEvent { }

public class OnRefreshShop : EventSystem<RefreshShopEvent>
{
    protected override void Handler(RefreshShopEvent self)
    {
        RefreshShop();
    }
}

FTask.OnceTimer(scene, 5000, new RefreshShopEvent());

// 热重载后:
// 1. EventComponent 实现了 IAssemblyLifecycle 接口
// 2. 旧程序集卸载时,旧的事件监听器被移除
// 3. 新程序集加载时,新的事件监听器被自动注册
// 4. 定时器触发时,发布事件到 EventComponent
// 5. EventComponent 调用新注册的监听器
// 6. 执行的是新程序集中的 RefreshShop() 方法
```

**热重载流程对比:**

| 步骤 | Action 方式 | 事件方式 |
|------|------------|---------|
| 定时器创建 | 捕获闭包引用 | 保存事件数据 |
| 程序集卸载 | 闭包引用不变 | 移除旧监听器 |
| 程序集加载 | 无影响 | 注册新监听器 |
| 定时器触发 | 执行旧闭包 | 发布事件 → 执行新监听器 |

**实际示例:**
```csharp
// 场景: 5 秒后刷新商店
public class ShopSystem
{
    private int _shopLevel = 1;

    public void ScheduleRefresh(Scene scene)
    {
        // ❌ Action 方式
        FTask.OnceTimer(scene, 5000, () =>
        {
            Log.Info($"商店等级: {_shopLevel}");
            // 热重载修改这行代码,仍会执行旧代码
        });

        // ✅ 事件方式
        FTask.OnceTimer(scene, 5000, new ShopRefreshEvent
        {
            ShopLevel = _shopLevel
        });
    }
}

public struct ShopRefreshEvent
{
    public int ShopLevel;
}

public class OnShopRefresh : EventSystem<ShopRefreshEvent>
{
    protected override void Handler(ShopRefreshEvent self)
    {
        Log.Info($"商店等级: {self.ShopLevel}");
        // 热重载修改这行代码,会执行新代码
    }
}
```

💡 **建议:**
- 开发阶段使用事件方式,方便热重载调试
- 生产环境两种方式性能差异不大,根据需求选择

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 优先使用 FTask 简化方法
public async FTask Example1(Scene scene)
{
    await FTask.Wait(scene, 1000);  // ✅ 推荐
}

// 2. 保存 timerId 以便取消
private long _timerId;
public void Example2(Scene scene)
{
    _timerId = FTask.OnceTimer(scene, 5000, Callback);  // ✅
}

// 3. 使用 ref 参数自动重置 ID
public void Example3(Scene scene)
{
    FTask.RemoveTimer(scene, ref _timerId);  // ✅ _timerId 自动置为 0
}

// 4. 合理使用取消令牌
public async FTask Example4(Scene scene, FCancellationToken cts)
{
    await FTask.Wait(scene, 10000, cts);  // ✅
}

// 5. 及时清理重复定时器
public void Example5(Scene scene)
{
    var timerId = FTask.RepeatedTimer(scene, 1000, Update);

    // 不需要时立即取消
    FTask.RemoveTimer(scene, ref timerId);  // ✅
}

// 6. 需要热重载的逻辑使用事件方式
public struct GameLogicEvent { }

public class OnGameLogic : EventSystem<GameLogicEvent>
{
    protected override void Handler(GameLogicEvent self)
    {
        // ✅ 这段代码支持热重载
        ExecuteGameLogic();
    }
}

public void Example6(Scene scene)
{
    // ✅ 使用事件方式,支持热重载
    FTask.OnceTimer(scene, 5000, new GameLogicEvent());
}

// 7. 框架级别逻辑可使用 Action 方式
public void Example7(Scene scene)
{
    // ✅ 框架级别的逻辑,不需要热重载
    FTask.OnceTimer(scene, 1000, () =>
    {
        CleanupResources();
    });
}
```

### ⚠️ 注意事项

```csharp
// 1. 不要忘记取消重复定时器
private long _repeatTimerId;
public void Bad1(Scene scene)
{
    _repeatTimerId = scene.TimerComponent.Net.RepeatedTimer(1000, Update);
    // ❌ 忘记取消,会一直执行
}

// 2. 不要在定时器回调中访问已销毁的对象
public void Bad2(Scene scene, Player player)
{
    scene.TimerComponent.Net.OnceTimer(5000, () =>
    {
        // ❌ player 可能已被销毁
        player.Health += 100;
    });

    // ✅ 正确做法: 检查对象是否存在
    scene.TimerComponent.Net.OnceTimer(5000, () =>
    {
        if (!player.IsDisposed)
        {
            player.Health += 100;
        }
    });
}

// 3. 不要创建过多的短周期定时器
public void Bad3(Scene scene)
{
    // ❌ 1000 个 10ms 定时器
    for (int i = 0; i < 1000; i++)
    {
        scene.TimerComponent.Net.RepeatedTimer(10, Update);
    }
}

// 4. OnceTillTimer 的 tillTime 要大于当前时间
public void Bad4(Scene scene)
{
    long pastTime = TimeHelper.Now - 10000;
    scene.TimerComponent.Net.OnceTillTimer(pastTime, Callback);
    // ❌ 会记录错误日志
}
```

---

## 总结

Timer 系统是 Fantasy Framework 的**核心任务调度组件**,提供了:

- **易用性**: 简洁的 API 设计,支持异步等待和回调两种模式
- **灵活性**: 支持一次性、重复、事件触发等多种定时器类型
- **高性能**: 基于有序时间列表和对象池优化
- **可靠性**: 异常保护、自动清理、取消令牌支持
- **集成性**: 与 Scene、Event 系统深度集成

**设计理念:**
通过高性能的定时器系统,简化游戏中的延时执行、周期任务、倒计时等常见逻辑,提升开发效率。

---

## 相关文档

- [01-ECS.md](01-ECS.md) - Entity-Component-System 详解
- [04-Event.md](04-Event.md) - Event 系统使用指南
- [03-Scene.md](03-Scene.md) - Scene 和 SubScene 使用
