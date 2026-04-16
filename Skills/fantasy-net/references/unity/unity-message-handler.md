# Unity 客户端：接收服务器主动推送 Handler

Unity 客户端接收服务器主动下发（服务器→客户端）普通消息的 Handler。框架通过 Source Generator 编译时自动注册，无需手动注册。

---

## 概述

服务器主动推送消息（如广播、状态同步、事件通知）时，客户端通过创建 Handler 类来接收并处理。

**与服务器端 Handler 的区别：**
- 基类签名不同：Unity 客户端为 `Message<Session, T>`，服务器端为 `Message<T>`
- Session 类型不同：Unity 使用客户端 `Session`，服务器使用服务器端 `Session`
- Handler 文件放在 Unity 项目的 `Assets/Scripts/` 下，不是服务器 Hotfix assembly
- 只处理服务器→客户端方向的消息（如 `G2C_` 前缀），不处理客户端→服务器方向

---

## Workflow

```
确认要接收的消息协议？
│
├─► 服务器单向推送（G2X_YourMessage，IMessage）──► Message<Session, T>
└─► 暂不支持 RPC（客户端不能作为 RPC 服务端）
```

### 第 1 步：确认生成的消息类已存在

Handler 依赖导出工具生成的 C# 消息类。先确认 `NetworkProtocolUnityDirectory`（Unity 协议输出目录）中已有对应的 `.cs` 文件。如果还没有，先参考 `references/protocol/export.md` 运行导出工具。

### 第 2 步：确定 Handler 文件位置

搜索 Unity 项目中已有的 Handler 文件（通常以 `Handler.cs` 结尾），参考其所在目录放置新文件，保持项目约定一致。通常结构如下：

```
Assets/
└── Scripts/
    └── Network/
        └── Handler/
            ├── G2C_LoginNotifyHandler.cs
            └── G2C_PlayerMoveNotifyHandler.cs
```

### 第 3 步：创建 Handler 类

使用 `sealed class`，继承 `Message<Session, T>`（注意：两个泛型参数）。

### 第 4 步：在 Editor 中编译验证

保存文件后，Unity Editor 会自动重新编译。确认 Console 无报错。

---

## Handler 类型

### Message\<Session, T\> — 接收服务器单向推送

```csharp
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

// 类名 = 消息名 + "Handler"，例如消息是 G2C_ChatNotify，类名就是 G2C_ChatNotifyHandler
public sealed class {消息名}Handler : Message<Session, {消息名}>
{
    protected override async FTask Run(Session session, {消息名} message)
    {
        // 根据 message.{字段名} 处理业务逻辑
        await FTask.CompletedTask;
    }
}
```

**注意基类签名：** `Message<Session, {消息名}>`，第一个泛型参数固定为 `Session`，第二个是要处理的消息类型。

---

## 注意事项

- Handler 必须是 `sealed class`，不能是泛型类
- 一条消息只能有一个 Handler，重复定义只会注册一个
- 不要手动注册，不要修改 `.g.cs` 生成文件
- Handler 中避免直接操作 Unity 主线程对象（如 `GameObject`）时产生线程问题；Fantasy Unity 版本已通过调度保证在主线程执行，但仍需注意对象生命周期
- 如需访问全局状态，推荐通过单例 Manager 或事件系统中转，避免 Handler 与 UI 强耦合
