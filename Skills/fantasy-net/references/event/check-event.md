# Event 系统代码检查

**本文件用于检查已有的 Event 代码是否正确。创建新事件请读 struct-event.md 或 entity-event.md。**

---

## 检查流程

```
确认要检查什么？
│
├─► Struct 事件专项检查 ───────────────────► 读「Struct 事件专项」
│
├─► Entity 事件专项检查 ───────────────────► 读「Entity 事件专项」
│
└─► 通用检查（所有事件都适用）─────────────► 读「通用检查」
```

---

## 通用检查（所有事件都适用）

#### 错误 1：监听器缺少 sealed

所有事件监听器必须是 `sealed class`，无论是 Struct 事件还是 Entity 事件。

```csharp
// ❌ 错误：缺少 sealed
public class OnXxx : EventSystem<XxxEvent> { }
public class OnXxxAsync : AsyncEventSystem<XxxEvent> { }

// ✅ 正确
public sealed class OnXxx : EventSystem<XxxEvent> { }
public sealed class OnXxxAsync : AsyncEventSystem<XxxEvent> { }
```

---

#### 错误 2：手动注册监听器

监听器由 Source Generator 编译时自动注册，不需要手动注册。

```csharp
// ❌ 错误：手动注册（不需要）
scene.EventComponent.Register(new OnXxx());

// ✅ 正确：只需定义监听器类，编译时自动注册
```

---

#### 错误 3：修改生成的 .g.cs 文件

不要修改任何 `.g.cs` 文件，它们会在编译时被重新生成。

---

#### 错误 4：异步发布忘记 await

```csharp
// ❌ 错误：忘记 await，事件可能未处理完就继续执行后续逻辑
scene.EventComponent.PublishAsync(new XxxEvent { });

// ✅ 正确：使用 await 等待所有异步监听器完成
await scene.EventComponent.PublishAsync(new XxxEvent { });
```

---

## Struct 事件专项

#### 错误：使用 class 而不是 struct

```csharp
// ❌ 错误：应该用 struct
public class {事件名}Event
{
    // 根据业务需求定义字段，用于传递事件参数
}

// ✅ 正确：使用 struct
public struct {事件名}Event
{
    // 根据业务需求定义字段，用于传递事件参数
}
```

---

#### 错误：发布时缺少必要字段赋值

```csharp
// ❌ 错误：事件定义了多个字段，但发布时只赋值了部分
scene.EventComponent.Publish(new {事件名}Event
{
    PlayerId = player.Id
    // 缺少其他已定义字段的赋值
});

// ✅ 正确：赋值所有定义的字段
scene.EventComponent.Publish(new {事件名}Event
{
    // 根据事件定义的字段赋值
    PlayerId = player.Id,
    Value = 100
});
```

---

## Entity 事件专项

#### 错误：为事件专门创建新的 Entity

```csharp
// ❌ 错误：专门为事件创建新的 Entity
public class PlayerLevelUpEvent : Entity
{
    // 根据业务需求定义属性...
}

var levelUpEvent = Entity.Create<PlayerLevelUpEvent>(scene);
scene.EventComponent.Publish(levelUpEvent, isDisposed: true);

// ✅ 正确：直接使用 Struct 事件
public struct PlayerLevelUpEvent
{
    // 根据业务需求定义字段，用于传递事件参数
}

scene.EventComponent.Publish(new PlayerLevelUpEvent
{
    // 根据事件定义的字段赋值
});
```

---

#### 错误：isDisposed 参数错误

```csharp
// ❌ 错误：Entity 还在其他逻辑中使用，但 isDisposed: true
scene.EventComponent.Publish(this, isDisposed: true);  // ❌ 会销毁 Entity，导致其他逻辑出错

// ✅ 正确：Entity 还在其他逻辑中使用，isDisposed: false
scene.EventComponent.Publish(this, isDisposed: false);  // ✅ 不销毁

// ✅ 正确：Entity 没有其他逻辑使用，可以 isDisposed: true
scene.EventComponent.Publish(entity, isDisposed: true);  // ✅ 处理完成后销毁

// ✅ 正确：异步发布，同样需要注意 isDisposed
await scene.EventComponent.PublishAsync(this, isDisposed: false);
await scene.EventComponent.PublishAsync(entity, isDisposed: true);
```

**判断依据：** 这个 Entity 发布事件后，是否还有其他逻辑在使用它？

- **有** → `isDisposed: false`
- **没有** → `isDisposed: true`
