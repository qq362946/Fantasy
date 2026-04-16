# Unity 客户端：安装与集成 Fantasy.Unity

Fantasy.Unity 通过 Unity Package Manager (UPM) 安装，包名 `com.fantasy.unity`。  
环境要求：Unity 2022.3.62+，Scripting Backend Mono / IL2CPP，.NET Standard 2.1。

## Workflow

```
Step 1：安装 Fantasy.Unity 包（三选一）
Step 2：配置编译符号（csc.rsp）
Step 3：初始化框架并连接服务器（二选一）
Step 4：导入协议文件
Step 5：发送消息 / 接收推送
```

---

## Step 1：安装包

**方式 A：OpenUPM（推荐）** — 编辑 `Packages/manifest.json`：

```json
{
  "scopedRegistries": [
    { "name": "package.openupm.com", "url": "https://package.openupm.com", "scopes": ["com.fantasy.unity"] }
  ],
  "dependencies": { "com.fantasy.unity": "2026.0.1013" }
}
```

> **版本号**：UPM 不支持 `*` / `latest`，必须填写精确版本。  
> - 指定版本：直接替换上方版本号（当前最新 `2026.0.1013`，可在 [OpenUPM](https://openupm.com/packages/com.fantasy.unity/) 查询最新版）  
> - 升级到最新：`Window` → `Package Manager` → 选中 `Fantasy.Unity` → 点击 `Update`

UI 操作安装：`Edit` → `Project Settings` → `Package Manager` 添加上方 Scoped Registry → `Window` → `Package Manager` → `+` → `Add package by name` → `com.fantasy.unity`。

**方式 B：Git URL** — `Package Manager` → `+` → `Add package from git URL`：

```
https://github.com/qq362946/Fantasy.git?path=Fantasy.Packages/Fantasy.Unity
```

**方式 C：本地路径**（同仓库开发）— `manifest.json` 中：

```json
{ "dependencies": { "com.fantasy.unity": "file:相对路径/Fantasy.Packages/Fantasy.Unity" } }
```

> **⚠️ 版本一致性确认（必须）**  
> `Fantasy.Unity` 版本必须与服务器端 `Fantasy-Net` NuGet 包版本完全一致，否则协议不兼容，运行时会出现消息解析错误。  
> 安装完成后，询问用户服务器端当前使用的 `Fantasy-Net` 版本，确认两侧版本号一致后再继续。

---

## Step 2：配置编译符号

在 `Assets/csc.rsp` 中添加（文件不存在则创建）：

```
-define:FANTASY_UNITY
```

WebGL 平台改为：`-define:FANTASY_UNITY;FANTASY_WEBGL`

> 也可通过 Unity 菜单 `Fantasy` → `Fantasy Settings` 图形化勾选。

---

## Step 3：初始化框架并连接服务器

读取 `references/unity/unity-connection.md`，按其中的 Workflow 询问用户并完成初始化与连接配置。

---

## Step 4：导入协议文件

1. 在服务器项目定义 `.proto` 文件（见 `references/protocol/index.md`）
2. 运行 `Fantasy.ProtocolExportTool` 生成 C# 代码
3. 将 `Generate/NetworkProtocol/` 下的文件复制到 `Assets/Scripts/Generate/NetworkProtocol/`

---

## Step 5：发送消息 / 接收推送

```csharp
// RPC 请求（导出工具自动生成扩展方法）
var response = await Runtime.Session.C2G_LoginGameRequest("player123");
if (response.ErrorCode != 0) { Log.Error($"登录失败: {response.ErrorCode}"); return; }

// 接收服务器推送（放在 Assets/Scripts/ 下，SourceGenerator 自动注册）
public sealed class G2C_NoticeHandler : Message<Session, G2C_Notice>
{
    protected override async FTask Run(Session session, G2C_Notice message)
    {
        Log.Debug($"收到公告: {message.Content}");
        await FTask.CompletedTask;
    }
}
```

---

## 常见问题

| 问题 | 解决 |
|---|---|
| WebGL 无法连接 | 检查：`FANTASY_WEBGL` 编译符号 / 服务器开启 WebSocket 端口 / 使用 `ws://` 或 `wss://` |
| IL2CPP 编译报错 | 检查：`FANTASY_UNITY` 编译符号 / Unity >= 2022.3.62 |
| SourceGenerator 不生效 | 检查 `csc.rsp` 有 `-define:FANTASY_UNITY`，手动触发一次重新编译 |
