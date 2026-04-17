# 开发行为准则 - Fantasy 场景示例

本文件提供 Karpathy Guidelines 在 Fantasy 框架中的具体应用示例。

## 1. Think Before Coding - 明确假设

### 场景 1：实现玩家登录功能

❌ **错误做法：**

用户："帮我实现玩家登录功能"

AI 直接写代码：
- 假设使用 Roaming 架构
- 假设 Gate→Map 路由
- 假设 Player Entity 挂在 Map Scene
- 假设 Fantasy.config 已配置 RoamingType
- 不询问任何细节，直接开始实现

**问题：** 用户可能是单服架构，或者使用 Address 而非 Roaming，导致代码完全不适用。

✅ **正确做法：**

AI："我需要先确认几点：

1. **架构模式：** 你的服务器是单服还是分布式？如果是分布式，Gate 后面连接的是 Map、Game 还是其他服务器？
2. **Entity 归属：** 登录后需要创建 Player Entity 吗？它应该挂在哪个 Scene 下？
3. **通信方式：** 需要用 Roaming 自动路由，还是用 Address 手动发送？
4. **配置状态：** 如果用 Roaming，Fantasy.config 中是否已经配置了对应的 RoamingType？

如果你只是想要最简单的单服登录（客户端→Gate，Gate 直接处理），我可以直接在 Gate 上实现 Handler，不涉及 Roaming 或 Address。"

**收益：** 避免返工，确保实现符合实际架构。

---

### 场景 2：实现跨服通信

❌ **错误做法：**

用户："我需要 Game 服务器通知 Map 服务器"

AI 直接选择 Address 或 Roaming，不呈现权衡。

✅ **正确做法：**

AI："有三种方式可以实现跨服通信，各有适用场景：

1. **Address（点对点 RPC）：** 适合已知目标 Entity 的 RuntimeId，需要等待响应的场景。需要先获取目标 Entity.Address。
2. **Roaming（客户端路由）：** 适合客户端消息需要自动转发到后端服务器的场景。需要在 Fantasy.config 配置 RoamingType。
3. **SphereEvent（发布订阅）：** 适合一对多通知，不需要响应的场景。订阅方需要调用 Subscribe。

你的场景是：
- Game 需要等待 Map 的响应吗？
- 是否已知 Map 上目标 Entity 的 Address？
- 是一对一通知还是一对多广播？

请告诉我具体需求，我会选择最合适的方式。"

**收益：** 用户理解权衡，选择最适合的方案。

---

## 2. Simplicity First - 避免过度设计

### 场景 1：实现背包系统

❌ **过度设计：**

用户："实现一个简单的背包系统"

AI 写了：
```csharp
// 定义抽象接口
public interface IInventoryStrategy
{
    bool CanAddItem(ItemData item);
    void AddItem(ItemData item);
}

// 工厂类
public class InventoryFactory
{
    public static IInventoryStrategy Create(InventoryType type) { ... }
}

// 配置系统
public class InventoryConfig
{
    public int MaxSlots { get; set; }
    public InventoryType Type { get; set; }
}

// 多种实现
public class NormalInventory : IInventoryStrategy { ... }
public class VIPInventory : IInventoryStrategy { ... }

// 完整的事件系统
public struct InventoryChangedEvent : IEvent { ... }
public class InventoryEventSystem : EventSystem<InventoryChangedEvent> { ... }
```

**问题：** 用户只要基础背包，这些抽象都是推测性的，可能永远用不到。

✅ **简洁实现：**

```csharp
// 用户只要基础背包，直接写 Component
public sealed class InventoryComponent : Entity
{
    public List<ItemData> Items = new();
    public int MaxSlots = 50;
}

// 需要初始化时再加 System
public class InventoryAwakeSystem : AwakeSystem<InventoryComponent>
{
    protected override void Awake(InventoryComponent self)
    {
        self.Items = new List<ItemData>();
    }
}
```

**何时该抽象？** 当用户明确说"支持多种背包类型"或"需要扩展不同规则"时。

**收益：** 50 行代码解决问题，而不是 200 行。

---

### 场景 2：实现定时器功能

❌ **过度设计：**

用户："实现一个 5 秒后执行的任务"

AI 写了自定义的定时器管理器、定时器配置类、定时器事件和 UpdateSystem 来驱动。

✅ **简洁实现：**

```csharp
// 直接用 Fantasy 内置的 Timer
await FTask.Wait(5000);
DoSomething();
```

或者需要取消时：

```csharp
var timerId = self.Scene.TimerComponent.OnceTimer(5000, () => DoSomething());
// 需要取消时：self.Scene.TimerComponent.RemoveTimer(ref timerId);
```

**收益：** 1 行代码解决问题，利用框架内置功能。

---

## 3. Surgical Changes - 精确修改

### 场景 1：修复 Handler 错误码

❌ **过度修改：**

用户："修复 LoginHandler 的错误码返回"

AI 改了：
- LoginHandler 的错误码 ✓（正确）
- 顺便重构了 Handler 的命名风格 ✗
- 调整了 using 顺序 ✗
- 把相邻的 RegisterHandler 也改成了新风格 ✗
- 删除了看起来没用的注释 ✗

✅ **精确修改：**

```csharp
// 只改这一行
- response.ErrorCode = 0;
+ response.ErrorCode = ErrorCode.LoginFailed;
```

**收益：** 清晰的 diff，易于 review，不引入意外风险。

---

### 场景 2：修改协议字段

❌ **过度修改：**

用户："给 C2G_Login 协议添加 DeviceId 字段"

AI 改了协议字段的同时，顺便给所有已有字段加注释、调整了 Response 的格式。

✅ **精确修改：**

```protobuf
// 只加这一行
  message C2G_Login // IRequest
  {
      string UserName = 1;
      string Password = 2;
+     string DeviceId = 3;
  }
```

**收益：** 不影响其他协议，不引入格式变更。

---

### Fantasy 特殊规则

**绝对不要修改的内容：**

1. **`.g.cs` 生成文件：** 即使发现问题，应该改源文件（`.proto` 或 Entity 定义）重新生成
2. **源码生成器的注册代码：** 如 `EntitySystemRegister.g.cs`、`MessageHandlerRegister.g.cs`
3. **现有的 Entity/Component 结构：** 除非用户明确要求重构

**可以清理的内容（仅限你的修改导致的）：**

- 你添加了新的 using，导致旧的 using 不再需要 → 可以删除
- 你删除了某个方法，导致变量不再使用 → 可以删除
- 不要删除修改前就已经存在的无关代码

---

## 4. Goal-Driven Execution - 可验证目标

### 场景 1：实现 Roaming 功能

❌ **模糊目标：**

用户："实现 Roaming 功能"

AI："好的，我来实现 Roaming"

然后开始写代码，不知道何时算完成，可能遗漏关键步骤。

✅ **可验证目标：**

AI："我将按以下步骤实现 Roaming，每步都有验证标准：

1. 定义协议 → verify: 运行 `dotnet fantasy-export`，成功生成 `.g.cs`
2. 配置 RoamingType → verify: `Fantasy.config` 中 `<roamingType>` 节点正确
3. 实现 Gate 侧 TryCreateRoaming → verify: `dotnet build` 编译通过，无 Handler 重复注册警告
4. 实现 Map 侧 OnCreateTerminus → verify: 编译通过，能访问 `LinkTerminusEntity`
5. 实现 Roaming Handler → verify: 检查 `MessageHandlerRegister.g.cs`，确认 Handler 已自动注册
6. 测试消息流转 → verify: Log.Debug 输出显示完整请求/响应链路

完成标准：客户端发送 IRoamingRequest，Map 收到并处理，客户端收到 response。"

**收益：** 清晰的进度，每步可独立验证，不会遗漏关键步骤。

---

### 场景 2：修复数据库查询 Bug

❌ **模糊目标：**

用户："数据库查询有问题，帮我修复"

AI："我来看看代码"（然后开始猜测问题，可能改错地方）

✅ **可验证目标：**

AI："我将按以下步骤排查和修复：

1. 复现问题 → verify: 添加 `Log.Debug` 输出查询条件，能稳定复现
2. 定位根因 → verify: 确认是 `scene.World.Database` 为 null / Entity 未实现 `ISupportedSerialize` / 查询条件错误 / 索引缺失
3. 修复代码 → verify: `dotnet build` 编译通过
4. 验证修复 → verify: 运行相同查询，Log 输出正确结果，无异常

完成标准：查询返回预期数据，Log 无错误。"

**收益：** 系统化排查，不会遗漏根因，修复后有明确的验证标准。

---

### Fantasy 验证方式总结

| 步骤类型 | 验证方式 |
|---|---|
| 协议定义 | `dotnet fantasy-export` 成功，生成 `.g.cs` 文件 |
| 配置修改 | XML 格式正确，节点引用关系正确 |
| 代码编写 | `dotnet build` 无错误，无警告 |
| Handler 注册 | 检查 `*Register.g.cs` 文件，确认自动注册 |
| 消息流转 | `Log.Debug` 输出关键节点，确认请求/响应正确 |
| 数据库操作 | 查询返回预期数据，Log 无异常 |
| 定时器 | `Log.Debug` 输出触发时间，确认精度和取消逻辑 |
| 事件系统 | `Log.Debug` 输出发布和监听，确认事件传递 |
