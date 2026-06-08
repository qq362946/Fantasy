# FantasyRuntime 组件使用指南

本指南将详细介绍 Fantasy 框架为 Unity 客户端提供的 **FantasyRuntime** 组件和 **Runtime 静态访问类**,它们可以大幅简化网络连接和框架初始化的代码。

---

## 目录

- [前置阅读](#前置阅读)
- [FantasyRuntime 简介](#fantasyruntime-简介)
  - [两种使用模式](#两种使用模式)
  - [核心特性](#核心特性)
- [快速开始](#快速开始)
  - [方式一: MonoBehaviour 组件模式](#方式一-monobehaviour-组件模式)
  - [方式二: Runtime 静态类模式](#方式二-runtime-静态类模式)
- [MonoBehaviour 组件模式详解](#monobehaviour-组件模式详解)
  - [组件配置参数](#组件配置参数)
  - [使用示例](#使用示例)
  - [Unity 事件回调](#unity-事件回调)
  - [访问 Ping 延迟](#访问-ping-延迟)
- [Runtime 静态类模式详解](#runtime-静态类模式详解)
  - [静态属性说明](#静态属性说明)
  - [Connect 方法](#connect-方法)
  - [使用示例](#使用示例-1)
  - [完整连接示例](#完整连接示例)
- [心跳系统](#心跳系统)
  - [心跳参数说明](#心跳参数说明)
  - [延迟监控](#延迟监控)
- [多实例管理](#多实例管理)
  - [场景说明](#场景说明)
  - [多服务器连接示例](#多服务器连接示例)
  - [实例标识](#实例标识)
- [常见使用场景](#常见使用场景)
  - [场景 1: 简单的单服务器游戏](#场景-1-简单的单服务器游戏)
  - [场景 2: 动态切换服务器](#场景-2-动态切换服务器)
  - [场景 3: 多服务器连接](#场景-3-多服务器连接)
  - [场景 4: 纯代码控制](#场景-4-纯代码控制)
- [最佳实践](#最佳实践)
  - [推荐做法](#推荐做法)
  - [不推荐做法](#不推荐做法)
- [常见问题](#常见问题)
- [下一步](#下一步)

---

## 前置阅读

在阅读本指南之前,建议先阅读:

1. ✅ [编写启动代码 - Unity 客户端](01-WritingStartupCode-Unity.md)
2. ✅ 了解 Fantasy 的基础初始化流程
3. ✅ 了解 Unity MonoBehaviour 生命周期

---

## FantasyRuntime 简介

`FantasyRuntime` 是 Fantasy 框架为 Unity 提供的**一站式网络连接和框架初始化组件**,旨在简化客户端代码并提供可视化配置能力。

### 两种使用模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| **MonoBehaviour 组件模式** | 将 `FantasyRuntime` 组件挂载到 GameObject 上,通过 Inspector 面板配置参数 | 需要可视化配置、快速原型开发、多个独立连接 |
| **Runtime 静态类模式** | 通过 `Runtime` 静态类全局访问 Scene、Session 和心跳组件 | 全局单例连接、快速访问网络对象、代码驱动场景 |

### 核心特性

✅ **自动初始化**: 自动调用 `Entry.Initialize()` 和 `Scene.Create()`
✅ **可视化配置**: 通过 Unity Inspector 面板配置网络参数
✅ **多协议支持**: TCP、KCP、WebSocket 三种协议
✅ **心跳管理**: 自动启用心跳组件,实时监控网络延迟
✅ **事件回调**: 支持连接成功/失败/断开的 UnityEvent 回调
✅ **多实例支持**: 可创建多个 FantasyRuntime 实例连接不同服务器
✅ **全局访问**: 通过 `Runtime` 静态类快速访问 Scene 和 Session

---

## 快速开始

### 方式一: MonoBehaviour 组件模式

最简单的使用方式,无需编写任何代码:

1. **在场景中创建一个空 GameObject**

   ```
   Hierarchy → 右键 → Create Empty → 命名为 "NetworkManager"
   ```

2. **添加 FantasyRuntime 组件**

   ```
   选中 NetworkManager → Add Component → 搜索 "FantasyRuntime"
   ```

3. **在 Inspector 面板配置参数**

   ```
   Network Settings:
   ├─ Remote IP: 127.0.0.1
   ├─ Remote Port: 20000
   └─ Protocol: TCP

   Connection Settings:
   ├─ Connect Timeout: 5000 (ms)
   └─ Receive JSON Log: ✗  (仅调试时启用)

   Heartbeat Settings:
   ├─ Enable Heartbeat: ✓
   ├─ Heartbeat Interval: 2000 (ms)
   ├─ Heartbeat Time Out: 30000 (ms)
   ├─ Heartbeat Time Out Interval: 5000 (ms)
   └─ Max Ping Samples: 4

   Runtime Settings:
   └─ Is Runtime Instance: ✓  (启用全局静态访问)
   ```

4. **运行游戏**

   - Unity 会在 `Start()` 时自动连接服务器
   - 连接成功后,可通过 `Runtime.Scene` 和 `Runtime.Session` 访问

5. **在其他脚本中访问**

   ```csharp
   using Fantasy;
   using Fantasy.Network;

   public class GameLogic : MonoBehaviour
   {
       private void Start()
       {
           // 通过 Runtime 静态类访问
           var session = Runtime.Session;
           var scene = Runtime.Scene;

           // 发送消息
           session.Send(new MyMessage());

           Log.Debug($"网络延迟: {Runtime.PingMilliseconds} ms");
       }
   }
   ```

---

### 方式二: Runtime 静态类模式

适合纯代码驱动的场景:

```csharp
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private void Start()
    {
        ConnectToServerAsync().Coroutine();
    }

    private async FTask ConnectToServerAsync()
    {
        try
        {
            // 使用 Runtime.Connect() 静态方法连接服务器
            // 自动完成 Fantasy 框架初始化 + Scene 创建 + 网络连接
            var session = await Runtime.Connect(
                remoteIP: "127.0.0.1",
                remotePort: 20000,
                protocol: FantasyRuntime.NetworkProtocolType.TCP,
                isHttps: false,
                connectTimeout: 5000,
                enableHeartbeat: true,
                heartbeatInterval: 2000,
                heartbeatTimeOut: 30000,
                heartbeatTimeOutInterval: 5000,
                maxPingSamples: 4,
                onConnectComplete: OnConnectComplete,
                onConnectFail: OnConnectFail,
                onConnectDisconnect: OnConnectDisconnect,
                enableReceiveMessageJsonLog: false
            );

            Log.Info("连接成功!");
        }
        catch (System.Exception ex)
        {
            Log.Error($"连接失败: {ex.Message}");
        }
    }

    private void OnConnectComplete()
    {
        Log.Info("连接成功回调");

        // 通过 Runtime 静态类访问
        var session = Runtime.Session;
        var scene = Runtime.Scene;

        // 发送消息
        session.Send(new MyMessage());
    }

    private void OnConnectFail()
    {
        Log.Error("连接失败回调");
    }

    private void OnConnectDisconnect()
    {
        Log.Warning("连接断开回调");
    }

    private void OnDestroy()
    {
        // 清理资源
        Runtime.OnDestroy();
    }
}
```

---

## MonoBehaviour 组件模式详解

### 组件配置参数

#### Instance Settings

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Runtime Name** | string | "FantasyRuntime" | 实例名称,用于日志输出和多实例标识 |

#### Network Settings

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Remote IP** | string | "127.0.0.1" | 服务器 IP 地址 |
| **Remote Port** | int | 20000 | 服务器端口号 |
| **Protocol** | Enum | TCP | 网络协议类型 (TCP/KCP/WebSocket) |
| **Enable Https** | bool | false | 是否启用 HTTPS (仅 WebSocket 有效) |

#### Connection Settings

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Connect Timeout** | int | 5000 | 连接超时时间 (单位: 毫秒) |
| **Receive JSON Log** | bool | false | 是否在客户端收到消息后打印消息 JSON,仅建议调试时启用 |

#### Heartbeat Settings

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Enable Heartbeat** | bool | true | 是否启用心跳组件 |
| **Heartbeat Interval** | int | 2000 | 心跳请求发送间隔 (单位: 毫秒) |
| **Heartbeat Time Out** | int | 30000 | 通信超时时间,超过此时间将断开连接 (单位: 毫秒) |
| **Heartbeat Time Out Interval** | int | 5000 | 检测连接超时的频率 (单位: 毫秒) |
| **Max Ping Samples** | int | 4 | Ping 包的采样数量,用于计算平均延迟 |

#### Runtime Settings

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Is Runtime Instance** | bool | false | 是否设置为全局 Runtime 实例,启用后可通过 `Runtime` 静态类访问 |

#### Event Callbacks

| 参数 | 类型 | 说明 |
|------|------|------|
| **On Connect Complete** | UnityEvent | 连接成功时触发 |
| **On Connect Fail** | UnityEvent | 连接失败时触发 |
| **On Connect Disconnect** | UnityEvent | 连接断开时触发 |

---

### 使用示例

#### 基础使用

```csharp
using Fantasy;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    [SerializeField] private FantasyRuntime fantasyRuntime;

    private void Start()
    {
        // FantasyRuntime 组件会在 Start() 时自动:
        // 1. 初始化 Fantasy 框架
        // 2. 创建 Scene
        // 3. 连接服务器
        // 4. 启用心跳组件 (如果配置了 Enable Heartbeat)

        // 无需手动编写初始化代码
    }

    public void SendMessage()
    {
        // 通过组件访问 Session
        if (fantasyRuntime.Session != null)
        {
            fantasyRuntime.Session.Send(new MyMessage());
        }
    }

    public void CheckPing()
    {
        // 通过组件访问 Ping 延迟
        try
        {
            int pingMs = fantasyRuntime.PingMilliseconds;
            Log.Info($"当前延迟: {pingMs} ms");
        }
        catch (System.InvalidOperationException ex)
        {
            Log.Warning($"心跳组件未初始化: {ex.Message}");
        }
    }
}
```

---

### Unity 事件回调

在 Inspector 面板中配置 UnityEvent 回调:

```
On Connect Complete:
├─ GameObject: UIManager
└─ Function: UIManager.OnNetworkConnected()

On Connect Fail:
├─ GameObject: UIManager
└─ Function: UIManager.OnNetworkFailed()

On Connect Disconnect:
├─ GameObject: UIManager
└─ Function: UIManager.OnNetworkDisconnected()
```

对应的 UI 管理器代码:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text connectionStatusText;
    [SerializeField] private Button retryButton;

    public void OnNetworkConnected()
    {
        connectionStatusText.text = "连接成功";
        connectionStatusText.color = Color.green;
        retryButton.gameObject.SetActive(false);

        Log.Info("UI: 连接成功");
    }

    public void OnNetworkFailed()
    {
        connectionStatusText.text = "连接失败";
        connectionStatusText.color = Color.red;
        retryButton.gameObject.SetActive(true);

        Log.Error("UI: 连接失败");
    }

    public void OnNetworkDisconnected()
    {
        connectionStatusText.text = "连接已断开";
        connectionStatusText.color = Color.yellow;
        retryButton.gameObject.SetActive(true);

        Log.Warning("UI: 连接断开");
    }
}
```

---

### 访问 Ping 延迟

通过 FantasyRuntime 组件实例访问网络延迟:

```csharp
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public class PingDisplay : MonoBehaviour
{
    [SerializeField] private FantasyRuntime fantasyRuntime;
    [SerializeField] private Text pingText;

    private void Update()
    {
        try
        {
            // 获取延迟 (毫秒)
            int pingMs = fantasyRuntime.PingMilliseconds;

            // 更新 UI
            pingText.text = $"Ping: {pingMs} ms";

            // 根据延迟设置颜色
            if (pingMs < 50)
                pingText.color = Color.green;
            else if (pingMs < 150)
                pingText.color = Color.yellow;
            else
                pingText.color = Color.red;
        }
        catch (System.InvalidOperationException)
        {
            // 心跳组件未初始化 (可能是 Enable Heartbeat 未勾选)
            pingText.text = "Ping: N/A";
            pingText.color = Color.gray;
        }
    }
}
```

---

## Runtime 静态类模式详解

`Runtime` 是一个静态类,提供对 Fantasy 核心对象的全局访问。

### 静态属性说明

| 属性 | 类型 | 说明 | 异常 |
|------|------|------|------|
| **Scene** | Scene | 获取当前 Scene 实例 | 未初始化时抛出 `InvalidOperationException` |
| **Session** | Session | 获取当前 Session 实例 | 未连接时抛出 `InvalidOperationException` |
| **SessionHeartbeatComponent** | SessionHeartbeatComponent | 获取心跳组件 | 未启用心跳时抛出 `InvalidOperationException` |
| **FantasyRuntime** | FantasyRuntime | 获取 FantasyRuntime 组件实例 | 未设置 isRuntimeInstance 时抛出 `InvalidOperationException` |
| **PingSeconds** | float | 获取网络延迟 (秒) | 未启用心跳时抛出 `InvalidOperationException` |
| **PingMilliseconds** | int | 获取网络延迟 (毫秒) | 未启用心跳时抛出 `InvalidOperationException` |

---

### Connect 方法

`Runtime.Connect()` 方法是一站式连接方法,自动完成所有初始化步骤。

#### 方法签名

```csharp
public static async FTask<Session> Connect(
    string remoteIP,
    int remotePort,
    FantasyRuntime.NetworkProtocolType protocol,
    bool isHttps,
    int connectTimeout,
    bool enableHeartbeat,
    int heartbeatInterval,
    int heartbeatTimeOut,
    int heartbeatTimeOutInterval,
    int maxPingSamples,
    Action onConnectComplete = null,
    Action onConnectFail = null,
    Action onConnectDisconnect = null,
    bool enableReceiveMessageJsonLog = false
)
```

#### 参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| **remoteIP** | string | 服务器 IP 地址 |
| **remotePort** | int | 服务器端口号 |
| **protocol** | NetworkProtocolType | 网络协议类型 (TCP/KCP/WebSocket) |
| **isHttps** | bool | 是否启用 HTTPS (仅 WebSocket 有效) |
| **connectTimeout** | int | 连接超时时间 (单位: 毫秒) |
| **enableHeartbeat** | bool | 是否启用心跳组件 |
| **heartbeatInterval** | int | 心跳请求发送间隔 (单位: 毫秒) |
| **heartbeatTimeOut** | int | 通信超时时间 (单位: 毫秒) |
| **heartbeatTimeOutInterval** | int | 检测连接超时的频率 (单位: 毫秒) |
| **maxPingSamples** | int | Ping 包的采样数量 (用于计算平均延迟) |
| **onConnectComplete** | Action | 连接成功回调 |
| **onConnectFail** | Action | 连接失败回调 |
| **onConnectDisconnect** | Action | 连接断开回调 |
| **enableReceiveMessageJsonLog** | bool | 是否在客户端收到消息后打印消息 JSON,默认 `false` |

#### 返回值

返回创建的 `Session` 实例。

---

### 使用示例

#### 最小示例

```csharp
using Fantasy;
using Fantasy.Async;
using UnityEngine;

public class SimpleConnection : MonoBehaviour
{
    private async void Start()
    {
        // 最简单的连接方式
        await Runtime.Connect(
            remoteIP: "127.0.0.1",
            remotePort: 20000,
            protocol: FantasyRuntime.NetworkProtocolType.TCP,
            isHttps: false,
            connectTimeout: 5000,
            enableHeartbeat: true,
            heartbeatInterval: 2000,
            heartbeatTimeOut: 30000,
            heartbeatTimeOutInterval: 5000,
            maxPingSamples: 4,
            enableReceiveMessageJsonLog: false
        );

        // 连接成功后,通过 Runtime 静态类访问
        Runtime.Session.Send(new MyMessage());

        Log.Info($"Ping: {Runtime.PingMilliseconds} ms");
    }

    private void OnDestroy()
    {
        Runtime.OnDestroy();
    }
}
```

---

### 完整连接示例

```csharp
using Fantasy;
using Fantasy.Async;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 20000;

    private void Start()
    {
        ConnectAsync().Coroutine();
    }

    private async FTask ConnectAsync()
    {
        try
        {
            Log.Info("开始连接服务器...");

            var session = await Runtime.Connect(
                remoteIP: serverIP,
                remotePort: serverPort,
                protocol: FantasyRuntime.NetworkProtocolType.TCP,
                isHttps: false,
                connectTimeout: 5000,
                enableHeartbeat: true,
                heartbeatInterval: 2000,
                heartbeatTimeOut: 30000,
                heartbeatTimeOutInterval: 5000,
                maxPingSamples: 4,
                onConnectComplete: OnConnected,
                onConnectFail: OnConnectionFailed,
                onConnectDisconnect: OnDisconnected,
                enableReceiveMessageJsonLog: false
            );

            Log.Info($"连接成功: {session.RemoteEndPoint}");
        }
        catch (System.Exception ex)
        {
            Log.Error($"连接异常: {ex.Message}");
        }
    }

    private void OnConnected()
    {
        Log.Info("回调: 连接成功");

        // 发送登录消息
        Runtime.Session.Send(new LoginRequest
        {
            Username = "Player123",
            Password = "password"
        });
    }

    private void OnConnectionFailed()
    {
        Log.Error("回调: 连接失败");

        // 显示重试 UI
        ShowRetryDialog();
    }

    private void OnDisconnected()
    {
        Log.Warning("回调: 连接断开");

        // 返回登录界面
        ReturnToLoginScreen();
    }

    private void ShowRetryDialog()
    {
        // TODO: 显示重试对话框
    }

    private void ReturnToLoginScreen()
    {
        // TODO: 返回登录界面
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    private void OnDestroy()
    {
        Runtime.OnDestroy();
    }
}
```

---

## 心跳系统

当启用心跳系统后,Fantasy 会自动向服务器发送心跳包,并计算网络延迟。

### 心跳参数说明

```csharp
// 心跳配置示例
enableHeartbeat: true,            // 启用心跳
heartbeatInterval: 2000,          // 每 2 秒发送一次心跳包
heartbeatTimeOut: 30000,          // 30 秒无通信则断开连接
heartbeatTimeOutInterval: 5000,   // 每 5 秒检测一次超时
maxPingSamples: 4                 // 采样 4 次 Ping 计算平均值
```

#### 参数详解

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| **heartbeatInterval** | 心跳请求发送间隔 (单位: 毫秒)<br>框架会定期向服务器发送心跳包<br>用于保持连接活跃和计算延迟 | 1000 - 5000 ms<br>(取决于网络质量) |
| **heartbeatTimeOut** | 通信超时时间 (单位: 毫秒)<br>超过此时间没有收到服务器响应,会自动断开连接<br>防止"僵尸连接" | 20000 - 60000 ms<br>(根据游戏类型调整) |
| **heartbeatTimeOutInterval** | 检测连接超时的频率 (单位: 毫秒)<br>框架每隔此时间检查一次连接是否超时 | heartbeatInterval 的 2-5 倍 |
| **maxPingSamples** | Ping 包的采样数量<br>框架会记录最近 N 次 Ping 值,计算平均延迟<br>样本越多,延迟越平滑,但反应越慢 | 3 - 10<br>(根据需求调整) |

---

### 延迟监控

心跳系统会自动计算网络延迟:

```csharp
using Fantasy;
using UnityEngine;
using UnityEngine.UI;

public class PingMonitor : MonoBehaviour
{
    [SerializeField] private Text pingText;

    private void Update()
    {
        try
        {
            // 方式 1: 获取延迟 (毫秒)
            int pingMs = Runtime.PingMilliseconds;
            pingText.text = $"Ping: {pingMs} ms";

            // 方式 2: 获取延迟 (秒)
            // float pingSeconds = Runtime.PingSeconds;
            // pingText.text = $"Ping: {pingSeconds:F3} s";

            // 根据延迟显示不同颜色
            if (pingMs < 50)
                pingText.color = Color.green;  // 优秀
            else if (pingMs < 100)
                pingText.color = Color.yellow; // 良好
            else if (pingMs < 200)
                pingText.color = Color.orange; // 一般
            else
                pingText.color = Color.red;    // 较差
        }
        catch (System.InvalidOperationException)
        {
            // 心跳组件未初始化
            pingText.text = "Ping: --";
            pingText.color = Color.gray;
        }
    }
}
```

---

## 多实例管理

Fantasy 支持创建多个 FantasyRuntime 实例,连接到不同的服务器。

### 场景说明

| 使用场景 | 说明 |
|---------|------|
| **单实例 + Runtime 全局访问** | 设置 `isRuntimeInstance = true`,通过 `Runtime` 静态类访问 |
| **多实例连接** | 创建多个 FantasyRuntime 组件,每个连接不同服务器 |
| **混合模式** | 一个全局实例 + 多个独立实例 |

---

### 多服务器连接示例

```csharp
using Fantasy;
using UnityEngine;

public class MultiServerManager : MonoBehaviour
{
    [Header("游戏服务器")]
    [SerializeField] private FantasyRuntime gameServerRuntime;

    [Header("聊天服务器")]
    [SerializeField] private FantasyRuntime chatServerRuntime;

    [Header("战斗服务器")]
    [SerializeField] private FantasyRuntime battleServerRuntime;

    private void Start()
    {
        // 三个 FantasyRuntime 组件会分别连接到不同的服务器
        // 在 Inspector 中配置各自的 IP/Port/Protocol

        // gameServerRuntime:
        //   - Remote IP: 192.168.1.100
        //   - Remote Port: 20000
        //   - Is Runtime Instance: true  (设置为全局实例)

        // chatServerRuntime:
        //   - Remote IP: 192.168.1.101
        //   - Remote Port: 20001
        //   - Is Runtime Instance: false

        // battleServerRuntime:
        //   - Remote IP: 192.168.1.102
        //   - Remote Port: 20002
        //   - Is Runtime Instance: false
    }

    public void SendGameMessage()
    {
        // 方式 1: 通过 Runtime 静态类访问 (因为设置了 isRuntimeInstance)
        Runtime.Session.Send(new GameMessage());

        // 方式 2: 通过组件实例访问
        // gameServerRuntime.Session.Send(new GameMessage());
    }

    public void SendChatMessage()
    {
        // 只能通过组件实例访问 (因为未设置 isRuntimeInstance)
        chatServerRuntime.Session.Send(new ChatMessage
        {
            Content = "Hello World"
        });
    }

    public void SendBattleMessage()
    {
        // 只能通过组件实例访问
        battleServerRuntime.Session.Send(new BattleMessage
        {
            Action = BattleAction.Attack
        });
    }

    public void CheckAllPings()
    {
        // 检查各服务器延迟
        Log.Info($"游戏服务器 Ping: {gameServerRuntime.PingMilliseconds} ms");
        Log.Info($"聊天服务器 Ping: {chatServerRuntime.PingMilliseconds} ms");
        Log.Info($"战斗服务器 Ping: {battleServerRuntime.PingMilliseconds} ms");
    }
}
```

---

### 实例标识

通过 `runtimeName` 参数标识不同的实例:

```
FantasyRuntime #1:
├─ Runtime Name: "GameServer"
├─ Remote IP: 192.168.1.100
└─ Remote Port: 20000

FantasyRuntime #2:
├─ Runtime Name: "ChatServer"
├─ Remote IP: 192.168.1.101
└─ Remote Port: 20001

FantasyRuntime #3:
├─ Runtime Name: "BattleServer"
├─ Remote IP: 192.168.1.102
└─ Remote Port: 20002
```

日志输出示例:

```
[GameServer] Connection established successfully to 192.168.1.100:20000
[GameServer] Heartbeat component started - Interval: 2000ms, Timeout: 30000ms

[ChatServer] Connection established successfully to 192.168.1.101:20001
[ChatServer] Heartbeat component started - Interval: 2000ms, Timeout: 30000ms

[BattleServer] Connection established successfully to 192.168.1.102:20002
[BattleServer] Heartbeat component started - Interval: 2000ms, Timeout: 30000ms
```

---

## 常见使用场景

### 场景 1: 简单的单服务器游戏

**需求:**
- 只连接一个游戏服务器
- 需要可视化配置
- 全局访问 Session

**推荐方案:**

```
1. 创建 GameObject → 添加 FantasyRuntime 组件
2. Inspector 配置参数
3. 勾选 "Is Runtime Instance"
4. 代码中使用 Runtime.Session
```

**示例:**

```csharp
using Fantasy;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    public void Login()
    {
        Runtime.Session.Send(new LoginRequest());
    }

    public void Attack()
    {
        Runtime.Session.Send(new AttackRequest());
    }
}
```

---

### 场景 2: 动态切换服务器

**需求:**
- 根据玩家选择连接不同服务器
- 纯代码控制
- 无需 Inspector 配置

**推荐方案:**

使用 `Runtime.Connect()` 动态连接:

```csharp
using Fantasy;
using Fantasy.Async;
using UnityEngine;

public class ServerSelector : MonoBehaviour
{
    public async FTask ConnectToServer(string serverIP, int serverPort)
    {
        // 断开旧连接
        Runtime.OnDestroy();

        // 连接新服务器
        await Runtime.Connect(
            remoteIP: serverIP,
            remotePort: serverPort,
            protocol: FantasyRuntime.NetworkProtocolType.TCP,
            isHttps: false,
            connectTimeout: 5000,
            enableHeartbeat: true,
            heartbeatInterval: 2000,
            heartbeatTimeOut: 30000,
            heartbeatTimeOutInterval: 5000,
            maxPingSamples: 4,
            enableReceiveMessageJsonLog: false
        );

        Log.Info($"已切换到服务器: {serverIP}:{serverPort}");
    }

    public void OnServerButtonClicked(int serverIndex)
    {
        // 服务器列表
        var servers = new[]
        {
            ("192.168.1.100", 20000), // 服务器 1
            ("192.168.1.101", 20000), // 服务器 2
            ("192.168.1.102", 20000)  // 服务器 3
        };

        var (ip, port) = servers[serverIndex];
        ConnectToServer(ip, port).Coroutine();
    }
}
```

---

### 场景 3: 多服务器连接

**需求:**
- 同时连接游戏服务器和聊天服务器
- 分别管理两个连接
- 不需要全局访问

**推荐方案:**

创建多个 FantasyRuntime 组件:

```csharp
using Fantasy;
using UnityEngine;

public class MultiServerClient : MonoBehaviour
{
    [SerializeField] private FantasyRuntime gameServerRuntime;
    [SerializeField] private FantasyRuntime chatServerRuntime;

    public void SendGameMessage(IMessage message)
    {
        gameServerRuntime.Session.Send(message);
    }

    public void SendChatMessage(IMessage message)
    {
        chatServerRuntime.Session.Send(message);
    }
}
```

---

### 场景 4: 纯代码控制

**需求:**
- 完全代码驱动,不使用 Inspector
- 游戏启动时动态连接
- 全局访问

**推荐方案:**

使用 `Runtime.Connect()` 静态方法:

```csharp
using Fantasy;
using Fantasy.Async;
using UnityEngine;

public class PureCodeClient : MonoBehaviour
{
    private async void Start()
    {
        // 读取配置文件或远程服务器列表
        var config = LoadServerConfig();

        // 连接服务器
        await Runtime.Connect(
            remoteIP: config.ServerIP,
            remotePort: config.ServerPort,
            protocol: config.Protocol,
            isHttps: config.IsHttps,
            connectTimeout: config.ConnectTimeout,
            enableHeartbeat: true,
            heartbeatInterval: 2000,
            heartbeatTimeOut: 30000,
            heartbeatTimeOutInterval: 5000,
            maxPingSamples: 4,
            onConnectComplete: OnConnected,
            onConnectFail: OnConnectionFailed,
            onConnectDisconnect: OnDisconnected,
            enableReceiveMessageJsonLog: config.EnableReceiveMessageJsonLog
        );
    }

    private ServerConfig LoadServerConfig()
    {
        // TODO: 从配置文件或远程 API 加载服务器配置
        return new ServerConfig
        {
            ServerIP = "127.0.0.1",
            ServerPort = 20000,
            Protocol = FantasyRuntime.NetworkProtocolType.TCP,
            IsHttps = false,
            ConnectTimeout = 5000
        };
    }

    private void OnConnected()
    {
        Log.Info("连接成功");
        Runtime.Session.Send(new LoginRequest());
    }

    private void OnConnectionFailed()
    {
        Log.Error("连接失败");
    }

    private void OnDisconnected()
    {
        Log.Warning("连接断开");
    }

    private void OnDestroy()
    {
        Runtime.OnDestroy();
    }
}

public class ServerConfig
{
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }
    public FantasyRuntime.NetworkProtocolType Protocol { get; set; }
    public bool IsHttps { get; set; }
    public int ConnectTimeout { get; set; }
    public bool EnableReceiveMessageJsonLog { get; set; }
}
```

---

## 最佳实践

### 推荐做法

✅ **单服务器游戏使用 Runtime 静态类**
```csharp
// 简洁直观
Runtime.Session.Send(new MyMessage());
```

✅ **多服务器使用 FantasyRuntime 组件实例**
```csharp
gameServerRuntime.Session.Send(new GameMessage());
chatServerRuntime.Session.Send(new ChatMessage());
```

✅ **启用心跳组件监控网络状态**
```csharp
// 推荐启用心跳组件
enableHeartbeat: true,
heartbeatInterval: 2000,    // 2 秒发送一次
heartbeatTimeOut: 30000,    // 30 秒超时
```

✅ **仅在调试时启用接收消息 JSON 日志**
```csharp
// 开发调试时可启用,上线保持默认 false
enableReceiveMessageJsonLog: true,
```

✅ **使用 UnityEvent 回调处理连接状态**
```csharp
// 在 Inspector 中配置 Unity 事件回调
// On Connect Complete → UIManager.ShowMainMenu()
// On Connect Fail → UIManager.ShowRetryDialog()
// On Connect Disconnect → UIManager.ReturnToLogin()
```

✅ **在 OnDestroy 中清理资源**
```csharp
private void OnDestroy()
{
    Runtime.OnDestroy();
}
```

✅ **异常处理**
```csharp
try
{
    int ping = Runtime.PingMilliseconds;
}
catch (InvalidOperationException ex)
{
    // 心跳组件未初始化
    Log.Warning($"无法获取 Ping: {ex.Message}");
}
```

---

### 不推荐做法

❌ **不要在多个地方都设置 isRuntimeInstance**
```csharp
// 错误: 多个 FantasyRuntime 都设置 isRuntimeInstance = true
// 只有最后一个会生效,导致混乱
```

❌ **不要忘记清理资源**
```csharp
// 错误: 未在 OnDestroy 中调用 Runtime.OnDestroy()
// 可能导致内存泄漏和网络连接未正确关闭
```

❌ **不要在未连接时访问 Session**
```csharp
// 错误: 未检查连接状态
Runtime.Session.Send(new MyMessage()); // 可能抛出异常

// 正确: 检查连接状态
if (Runtime.Session != null && !Runtime.Session.IsDisposed)
{
    Runtime.Session.Send(new MyMessage());
}
```

❌ **不要在 WebGL 平台使用 TCP/KCP**
```csharp
// 错误: WebGL 只支持 WebSocket
protocol: FantasyRuntime.NetworkProtocolType.TCP  // 在 WebGL 上会失败

// 正确:
#if UNITY_WEBGL
protocol: FantasyRuntime.NetworkProtocolType.WebSocket
#else
protocol: FantasyRuntime.NetworkProtocolType.TCP
#endif
```

❌ **不要禁用心跳组件后访问 Ping**
```csharp
// 错误: 禁用心跳组件后访问 Ping
enableHeartbeat: false,
...
int ping = Runtime.PingMilliseconds; // 抛出 InvalidOperationException

// 正确: 启用心跳组件
enableHeartbeat: true,
```

---

## 常见问题

### Q1: 如何判断是否已连接?

**回答:**

```csharp
// 方式 1: 使用 try-catch
try
{
    var session = Runtime.Session;
    if (!session.IsDisposed)
    {
        // 已连接且未释放
    }
}
catch (InvalidOperationException)
{
    // 未连接
}

// 方式 2: 通过组件实例判断
if (fantasyRuntime.Session != null && !fantasyRuntime.Session.IsDisposed)
{
    // 已连接且未释放
}
```

---

### Q2: Runtime.Session 抛出 InvalidOperationException

**错误信息:**
```
InvalidOperationException: Fantasy Session is not connected.
Please call 'Runtime.Connect()' to establish a connection before accessing Runtime.Session.
```

**原因:**
- 未调用 `Runtime.Connect()` 建立连接
- 或者未设置 FantasyRuntime 组件的 `isRuntimeInstance = true`

**解决:**

```csharp
// 方式 1: 使用 Runtime.Connect()
await Runtime.Connect(...);

// 方式 2: 设置 FantasyRuntime 的 isRuntimeInstance = true
// 在 Inspector 面板中勾选 "Is Runtime Instance"
```

---

### Q3: 无法获取 Ping 延迟

**错误信息:**
```
InvalidOperationException: Heartbeat component is not initialized.
Please enable heartbeat in FantasyInitialize settings...
```

**原因:**
- 未启用心跳组件 (`enableHeartbeat = false`)

**解决:**

```csharp
// MonoBehaviour 模式: 在 Inspector 中勾选 "Enable Heartbeat"
// Runtime 静态模式: 设置 enableHeartbeat: true
await Runtime.Connect(
    ...
    enableHeartbeat: true,  // ✅ 必须启用
    heartbeatInterval: 2000,
    ...
);
```

---

### Q4: 多个 FantasyRuntime 组件冲突

**问题:**
创建了多个 FantasyRuntime 组件,但 `Runtime.Session` 指向错误的实例。

**原因:**
多个组件都设置了 `isRuntimeInstance = true`,只有最后一个会生效。

**解决:**

```
只有一个 FantasyRuntime 应该设置 isRuntimeInstance = true
其他实例通过组件引用访问

示例:
├─ GameServer (FantasyRuntime)
│   └─ Is Runtime Instance: ✓  (全局访问)
├─ ChatServer (FantasyRuntime)
│   └─ Is Runtime Instance: ✗  (组件访问)
└─ BattleServer (FantasyRuntime)
    └─ Is Runtime Instance: ✗  (组件访问)
```

---

### Q5: 连接成功但心跳组件未启动

**问题:**
连接成功,但无法获取 Ping 延迟。

**原因:**
- 未启用心跳组件 (`enableHeartbeat = false`)
- 或者服务器不支持心跳协议

**解决:**

```csharp
// 1. 确保启用心跳组件
enableHeartbeat: true,

// 2. 检查服务器是否支持心跳协议
// Fantasy 服务器端需要实现心跳消息的处理

// 3. 检查日志输出
// 如果启用成功,会看到:
// [Fantasy] Heartbeat component started - Interval: 2000ms, Timeout: 30000ms
```

---

### Q6: WebGL 平台连接失败

**问题:**
WebGL 平台无法连接服务器。

**原因:**
- WebGL 只支持 WebSocket 协议
- 使用了 TCP 或 KCP 协议

**解决:**

```csharp
#if UNITY_WEBGL
var protocol = FantasyRuntime.NetworkProtocolType.WebSocket;
#else
var protocol = FantasyRuntime.NetworkProtocolType.TCP;
#endif

await Runtime.Connect(
    remoteIP: "example.com",
    remotePort: 20000,
    protocol: protocol,  // WebGL 必须使用 WebSocket
    isHttps: true,       // HTTPS 服务器设置为 true
    ...
);
```

---

### Q7: 如何重新连接服务器?

**回答:**

```csharp
using Fantasy;
using Fantasy.Async;
using UnityEngine;

public class ReconnectManager : MonoBehaviour
{
    public async FTask Reconnect()
    {
        // 1. 断开旧连接
        Runtime.OnDestroy();

        // 2. 等待一段时间 (可选)
        await FTask.Delay(1000);

        // 3. 重新连接
        await Runtime.Connect(
            remoteIP: "127.0.0.1",
            remotePort: 20000,
            protocol: FantasyRuntime.NetworkProtocolType.TCP,
            isHttps: false,
            connectTimeout: 5000,
            enableHeartbeat: true,
            heartbeatInterval: 2000,
            heartbeatTimeOut: 30000,
            heartbeatTimeOutInterval: 5000,
            maxPingSamples: 4
        );

        Log.Info("重新连接成功");
    }

    public void OnRetryButtonClicked()
    {
        Reconnect().Coroutine();
    }
}
```

---

## 下一步

现在你已经掌握了 FantasyRuntime 组件的使用,接下来可以:

1. 📖 阅读 [网络开发](09-Network.md) 学习消息发送和处理 (待完善)
2. 🔧 阅读 [协议定义](11-Protocol.md) 学习 .proto 文件 (待完善)
3. 🎮 阅读 [ECS 系统](06-ECS.md) 学习实体组件系统 (待完善)
4. 📚 查看 `Examples/Client/Unity` 目录下的完整示例

---

## 获取帮助

- **GitHub**: https://github.com/qq362946/Fantasy
- **文档**: https://www.code-fantasy.com/
- **Issues**: https://github.com/qq362946/Fantasy/issues

---

**祝你开发愉快!** 🎉
