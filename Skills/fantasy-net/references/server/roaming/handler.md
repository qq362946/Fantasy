# Roaming Handler 入口

**本文件只做分流。** 当用户要实现漫游消息处理、后端推送、跨服发送或 Terminus 传送时，先在这里判断应该读哪个子文档。

## Workflow

```text
实现 Roaming Handler / 后端推送 / 跨服发送 -> messaging.md
实现 Terminus 传送 -> transfer.md
```

## 必记规则

1. 客户端发 Roaming 消息时，Gate 无需手写转发代码
2. Handler 的实体类型取决于是否调用了 `LinkTerminusEntity`
3. 后端推送客户端、跨服发送都优先通过 TerminusHelper 扩展方法
4. 传送成功后原实例会被销毁，不要继续使用旧实例

## 子文档

- `messaging.md` - 漫游消息处理、后端推送、跨服发送
- `transfer.md` - Terminus 传送与生命周期
