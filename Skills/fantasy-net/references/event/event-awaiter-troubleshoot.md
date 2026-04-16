# EventAwaiter 排错清单

**适用场景：** 用户给的是现有 `EventAwaiter` 代码，或者在问为什么等不到、为什么超时、为什么没回调。

#### 错误 1：忘记添加 `EventAwaiterComponent`

```csharp
var result = await player.EventAwaiterComponent.Wait<TestEvent>();
```

如果 `AwakeSystem` 里没加组件，这里会直接出问题。

---

#### 错误 2：`Wait<T>()` 和 `Notify<T>()` 类型不一致

```csharp
await player.EventAwaiterComponent.Wait<Event1>();
player.EventAwaiterComponent.Notify(new Event2());
```

这种情况下永远等不到。

---

#### 错误 3：等待和通知发生在不同实体上

```csharp
await player1.EventAwaiterComponent.Wait<TestEvent>();
player2.EventAwaiterComponent.Notify(new TestEvent());
```

无效。必须是同一个 `EventAwaiterComponent`。

---

#### 错误 4：忘记处理非成功状态

不要默认 `Wait<T>()` 一定成功。`Timeout`、`Cancel`、`Destroy` 都应该进入业务分支。

---

#### 错误 5：不设置超时导致流程悬挂

对玩家交互、网络请求、跨服响应等场景，通常应设置超时。

---

#### 错误 6：在已销毁实体上继续等待

如果承载组件的实体被销毁，等待会结束并返回 `Destroy`，不是继续有效。

---

#### 错误 7：把广播场景错写成 `EventAwaiter`

如果真正需求是解耦广播、多个监听器独立消费、没有等待返回值，就改用 `Event`。

## 优先检查顺序

1. 是否真正添加了 `EventAwaiterComponent`
2. `Wait<T>()` / `Notify<T>()` 的泛型类型是否完全一致
3. 等待与通知是否发生在同一个实体实例上
4. 是否因为超时、取消、销毁提前结束
5. 是否把本应使用 `Event` 的广播场景错写成了 `EventAwaiter`
