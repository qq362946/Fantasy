# SphereEvent 最佳实践与排错

## 机制选择

### SphereEvent vs Event

- `Event`：单个 Scene 内部、本地进程内事件
- `SphereEvent`：跨 Scene、跨服务器的事件通知

### SphereEvent vs Roaming

- `SphereEvent`：服务器间发布-订阅，一对多
- `Roaming`：客户端经 Gate 访问后端服务，偏点对点路由

### SphereEvent vs Address

- `SphereEvent`：发布方不需要知道每个订阅方的具体业务实体
- `Address`：明确知道目标 Scene / Entity Address 时做一对一通信

## 最佳实践

1. 事件对象优先使用对象池：`SphereEventArgs.Create<T>(isFromPool: true)`
2. 发布时优先 `isAutoDisposed: true`
3. 事件对象只放必要字段，不要塞大对象和大数组
4. 订阅关系优先在 Scene 初始化路径建立
5. Scene 销毁前调用 `SphereEventComponent.Close()` 清理订阅关系
6. 需要热重载的跨服业务事件，SphereEvent 是合适选择

## 常见错误

### 错误 1：事件类缺少 `[MemoryPackable]` 或 `partial`

2025.2.1410+ 版本里这是编译错误。

### 错误 2：直接 `new` 事件对象

优先使用：

```csharp
var eventArgs = SphereEventArgs.Create<TestSphereEvent>(isFromPool: true);
```

### 错误 3：把超大数据直接塞进事件

跨服事件是要序列化并走网络的。优先只传 ID、类型、少量关键字段，需要详细数据时再查数据库或走其他链路。

### 错误 4：忘记清理订阅关系

Scene 销毁、服务器关闭、重置逻辑时，记得：

```csharp
await scene.SphereEventComponent.Close();
```

### 错误 5：把订阅方取消订阅和发布方移除订阅者的语义混用

重点区分：

- 订阅方主动取消自己的订阅 -> `Unsubscribe<T>(remoteAddress)`
- 发布方主动撤销某个远程订阅者 -> `RevokeRemoteSubscriber<T>(subscriberAddress)`
- 仅本地移除远程订阅记录，不通知远端 -> `UnregisterRemoteSubscriber<T>(subscriberAddress)`

如果当前代码是“我之前 `Subscribe<T>(remoteAddress)` 了，现在我要取消自己的订阅”，优先检查是否错误用了 `UnregisterRemoteSubscriber`。

## 常见问题

### 会不会重复订阅

框架内部会避免相同订阅关系重复建立，但业务上仍然应把订阅放在明确的初始化位置，而不是任意调用路径里。

### 订阅方断线怎么办

断线或重建后通常需要重新订阅。发布方侧如需整体清理关系，可用 `Close()` 或按需撤销远程订阅者。

### 如何检查取消订阅写法是否正确

看最初是谁发起的动作：

- 如果本地代码调用的是 `Subscribe<T>(remoteAddress)`，那本地后续取消通常应看 `Unsubscribe<T>(remoteAddress)`
- 如果本地代码是发布方，要从自己的远端订阅者列表里移除某个订阅者，才看 `RevokeRemoteSubscriber` / `UnregisterRemoteSubscriber`

### 是否支持热重载

支持。SphereEvent 处理器会跟随程序集生命周期更新，订阅关系本身不等于处理器代码本体。

### 发布时多个处理器怎么执行

多个处理器可能并发执行。处理器内部如果依赖顺序或共享状态，自己加协程锁控制。

## 排查顺序

1. 事件类是否正确继承 `SphereEventArgs`
2. 是否加了 `[MemoryPackable]` 和 `partial`
3. 是否通过 `SphereEventArgs.Create<T>()` 创建对象
4. 订阅是否真的在运行时建立成功
5. 发布使用的 Address 是否是正确远程 Scene Address
6. 是否因为 Scene 销毁或 `Close()` 导致订阅关系已经清理
7. 是否本来更适合用 Event / Roaming / Address 而不是 SphereEvent
