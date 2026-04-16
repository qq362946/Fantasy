# Entity 事件完整流程

**适用场景：** 需要传递现有 Entity（玩家、怪物、物品等）时使用。

**重要：** 不要为了使用 Entity 事件而专门创建新的 Entity，直接使用 Struct 事件即可。

---

## 第 1 步：创建监听器

### 同步监听器

```csharp
using Fantasy.Event;

// 监听现有 Entity 类型
public class On{Entity名称} : EventSystem<{Entity名称}>
{
    protected override void Handler({Entity名称} self)
    {
        // self 就是传递过来的 Entity
        // 业务逻辑
    }
}
```

**适用场景：** UI 更新、简单计算、状态修改。

---

### 异步监听器

```csharp
using Fantasy.Event;
using Fantasy.Async;

public class {业务逻辑}On{Entity名称} : AsyncEventSystem<{Entity名称}>
{
    protected override async FTask Handler({Entity名称} self)
    {
        // self 就是传递过来的 Entity
        // 异步操作
        await SaveToDatabase(self.Id);
    }
}
```

**适用场景：** 数据库操作、网络请求、文件 IO。

**注意：** 
- 一个事件可以有多个监听器，同步监听器顺序执行，异步监听器并行执行
- 监听器之间不应有执行顺序依赖

---

## 第 2 步：发布现有 Entity

### 同步发布

```csharp
// 获取或创建现有的 Entity
var player = ...; // 通过 scene.GetEntity<Player>() 或其他方式获取

// isDisposed 参数非常重要：
// - false: 不销毁 Entity（推荐，因为 Entity 还在其他系统使用）
// - true: 处理完成后自动销毁 Entity（仅当确定不再使用时）
scene.EventComponent.Publish(player, isDisposed: false);
```

**`isDisposed` 参数说明：**
- `false`（推荐）：Entity 还在其他系统使用，不销毁
- `true`：确定 Entity 不再使用，处理完成后自动销毁

---

### 异步发布（等待所有异步监听器完成）

```csharp
// 获取或创建现有的 Entity
var player = ...; // 通过 scene.GetEntity<Player>() 或其他方式获取

// 等待所有异步监听器完成
await scene.EventComponent.PublishAsync(player, isDisposed: false);
```

---

## 完整示例

```csharp
// 1. 定义 Player Entity（已存在）
public sealed class Player : Entity
{
    public string Name;
    public int Level;
    public int Health;
}

// 2. 创建监听器
public class OnPlayerLevelUp : EventSystem<Player>
{
    protected override void Handler(Player self)
    {
        Log.Info($"玩家 {self.Name} 升级到 {self.Level} 级");
        // 更新 UI
    }
}

// 3. 创建异步监听器
public class SavePlayerOnLevelUp : AsyncEventSystem<Player>
{
    protected override async FTask Handler(Player self)
    {
        // 保存玩家数据
        await self.Save();
    }
}

// 4. 在业务逻辑中发布现有 Entity（同步）
public class Player : Entity
{
    public void LevelUp()
    {
        Level++;
        
        // 发布自己（this）
        // isDisposed: false 因为 Player 还在使用中
        Scene.EventComponent.Publish(this, isDisposed: false);
    }
}

// 5. 在业务逻辑中发布现有 Entity（异步）
public class Player : Entity
{
    public async FTask LevelUpAsync()
    {
        Level++;
        
        // 等待所有异步监听器完成
        // isDisposed: false 因为 Player 还在使用中
        await Scene.EventComponent.PublishAsync(this, isDisposed: false);
    }
}
```

---

## 注意事项

- 监听器必须是 `sealed class`，不能是泛型类
- 监听器应与 Entity 数据定义分离：多 assembly 项目放在逻辑层 assembly，单 assembly 项目按文件夹分离即可
- 不要手动注册监听器，不要修改 `.g.cs` 生成文件
- **`isDisposed` 参数非常重要**：如果 Entity 还在其他系统使用，必须传 `false`，否则会导致 Entity 被销毁
- 单个监听器的错误不会影响其他监听器，框架会捕获异常并记录日志
- **不要为了使用 Entity 事件而专门创建新的 Entity**，直接使用 Struct 事件即可

---

## 相关文档

- `index.md` - Event 系统入口和 Workflow
- `struct-event.md` - Struct 事件完整流程（推荐大多数场景使用）
- `check-event.md` - Event 系统代码检查清单
