# Unity 客户端入口

**本文件只做分流。** 涉及 Fantasy Unity 客户端安装、连接服务器、发送消息、接收服务器推送时，先在这里判断要读哪个子文档。

## Workflow

```text
安装 Fantasy.Unity / 初次接入项目 -> setup-unity.md
初始化框架并连接服务器 -> unity-connection.md
连接成功后发送 IMessage / IRequest -> unity-session.md
接收服务器主动推送 -> unity-message-handler.md
```

## 必记规则

1. `Fantasy.Unity` 版本必须与服务器端 `Fantasy-Net` 版本一致
2. Unity 侧必须配置 `FANTASY_UNITY`，WebGL 还要加 `FANTASY_WEBGL`
3. 连接方式先在 `FantasyRuntime`、`scene.Connect`、`Runtime.Connect` 三者之间做选择
4. 协议代码来自导出工具生成结果，不要手写或改生成文件

## 子文档

- `setup-unity.md` - 安装、编译符号、导入协议
- `unity-connection.md` - 连接方式与协议选择
- `unity-session.md` - Session 发送消息、RPC、断开连接
- `unity-message-handler.md` - 接收服务器主动推送
