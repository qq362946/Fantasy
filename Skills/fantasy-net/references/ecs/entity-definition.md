# Entity / Component 定义

## 先决定要不要配套 System

创建 Entity 时，根据需求判断要不要配套 System，不要无脑生成空 System。

| 配套 System | 何时生成 |
|---|---|
| `AwakeSystem` | 有初始化逻辑时，如设置默认值、注册订阅、启动计时器 |
| `DestroySystem` | 有自定义字段时必须生成；有清理逻辑时也生成 |
| `UpdateSystem` | 用户明确需要每帧执行逻辑时 |
| `DeserializeSystem` | Entity 实现 `ISupportedSerialize` 且反序列化后要重建运行时状态时 |
| `TransferOutSystem` / `TransferInSystem` | Entity 需要跨服传送时 |

如果用户没有提供足够信息判断某个 System 是否需要，先问清楚，再生成。

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
- `lifecycle.md` - 各种配套 System 的具体写法
- `scene.md` - Scene 与 Entity 的归属关系
