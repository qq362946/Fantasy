# EventAwaiter 建模规则

**适用场景：** 还没决定 `EventAwaiterComponent` 挂在哪个实体，或不确定要不要建业务会话实体。

## 规则 1：谁拥有等待流程，就挂到谁身上

如果等待属于某个明确业务对象，就把组件挂到那个业务对象上。

- 玩家确认 -> `Player`
- 一次交易 -> `TradeSession`
- 一次组队邀请 -> `TeamInvite`
- 一次跨服请求 -> `CrossServerRequest`
- 一次资源加载 -> `ResourceLoadRequest`

## 规则 2：多人围绕同一流程等待时，创建会话实体

如果真实模型是多个参与者围绕同一业务状态变化协作，不要把等待分散挂到多个不相干实体上，创建一个专门的会话实体承载 `EventAwaiterComponent`。

```csharp
public sealed class TradeSession : Entity
{
    public EventAwaiterComponent EventAwaiterComponent { get; private set; }
}
```

## 规则 3：默认不要挂到 `Scene`

只有在确实是全局等待场景时才考虑挂到 `Scene`。大多数业务等待都应该挂在更具体的业务实体上。

## 规则 4：事件数据尽量小而明确

推荐：

- 字段表达最小必要上下文
- 传 ID、状态、少量值类型字段
- 需要大量上下文时，传 ID 后再从 `Scene` 或业务实体取完整数据

不推荐：

- 在 struct 事件里塞大对象图
- 用不同事件类型硬编码不同玩家或不同实例

## 规则 5：流程结束后及时销毁会话实体

这类实体通常是一次性的。流程完成后，直接释放，避免悬挂状态残留。

## 规则 6：取消和超时都是正常业务分支

不要只写成功路径。上层流程结束、玩家离线、并发等待提前失败，都可能触发取消或销毁。
