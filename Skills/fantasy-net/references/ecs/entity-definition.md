# Entity / Component 定义

## 先决定要不要生命周期 System

创建 Entity 时，根据需求判断要不要配套生命周期 System，不要无脑生成空 System。

| 生命周期 System | 何时生成 |
|---|---|
| `AwakeSystem` | 有初始化逻辑时，如设置默认值、注册订阅、启动计时器 |
| `DestroySystem` | 有自定义字段时必须生成；有清理逻辑时也生成 |
| `UpdateSystem` | 用户明确需要每帧执行逻辑时 |
| `DeserializeSystem` | Entity 实现 `ISupportedSerialize` 且反序列化后要重建运行时状态时 |
| `TransferOutSystem` / `TransferInSystem` | Entity 需要跨服传送时 |

如果用户没有提供足够信息判断某个生命周期 System 是否需要，先问清楚，再生成。

## ComponentSystem 与 Helper

Component 有业务方法时，把实现写成扩展方法，配套类名必须是 `{Component完整类名}System`：

```csharp
public static class HealthComponentSystem
{
    public static void Damage(this HealthComponent self, int value)
    {
        self.CurrentHp = Math.Max(0, self.CurrentHp - value);
    }
}
```

只有其他系统需要通过更自然的业务对象调用、隐藏重复的 `GetComponent`，或需要组合多个 Component 时，才增加 `{业务名}Helper`：

```csharp
public static class HealthHelper
{
    public static void Damage(Player target, int value)
        => target.GetComponent<HealthComponent>().Damage(value);
}
```

- 实际组件逻辑放在 `HealthComponentSystem`，Helper 只提供跨系统入口，不重复实现
- 调用方已经持有 `HealthComponent`，或没有跨系统调用时，不创建 Helper
- Helper 只暴露外部真正需要的方法，不要机械包装 ComponentSystem 的全部方法
- Helper 通常直接使用 `static class`，不要为单一实现再创建 `IHealthHelper`
- 生命周期类仍按框架类型命名，例如 `HealthComponentAwakeSystem`、`HealthComponentDestroySystem`

## 存放位置

Entity / Component 定义应与 System 逻辑分离。

推荐目录：

```text
YourProject/
├── Entity/
└── System/
```

如果用户没有明确偏好，先问希望放在哪个目录或哪个程序集下，不要自行假设。

## 定义示例

```csharp
public sealed class Player : Entity
{
    public string Name;
    public int Level;
}

public sealed class HealthComponent : Entity
{
    public int MaxHp;
    public int CurrentHp;
}

public sealed class Account : Entity, ISupportedDataBase
{
    public string Username;
    public string Password;
}
```

## 框架自动维护的关键属性

| 属性 | 说明 |
|---|---|
| `Id` | 持久化 ID |
| `RuntimeId` | 运行时 ID |
| `Scene` | 所属 Scene |
| `Parent` | 父 Entity |
| `IsDisposed` | 是否已销毁 |

## 相关文档

- `entity-operations.md` - 定义后如何创建和操作
- `lifecycle.md` - 各种生命周期 System 的具体写法
- `scene.md` - Scene 与 Entity 的归属关系
