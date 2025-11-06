# Fantasy.config 配置文件详解

本文档详细介绍 Fantasy Framework 的核心配置文件 `Fantasy.config` 的所有参数及其用法。

## 目录

- [配置文件概述](#配置文件概述)
- [完整配置示例](#完整配置示例)
- [配置参数详解](#配置参数详解)
  - [1. 网络配置 (network)](#1-网络配置-network)
  - [2. 会话配置 (session)](#2-会话配置-session)
  - [3. 机器配置 (machine)](#3-机器配置-machine)
  - [4. 进程配置 (process)](#4-进程配置-process)
  - [5. 世界配置 (world)](#5-世界配置-world)
  - [6. 场景配置 (scene)](#6-场景配置-scene)
  - [7. 单位配置 (unit)](#7-单位配置-unit)
- [配置最佳实践](#配置最佳实践)
- [配置验证](#配置验证)

---

## 配置文件概述

`Fantasy.config` 是 Fantasy Framework 的核心配置文件，采用 XML 格式。它定义了：

- 🌐 **网络通信配置**：内部通信协议、消息大小限制
- 🔌 **会话管理配置**：超时检测、心跳间隔
- 🖥️ **机器配置**：服务器 IP 地址、端口绑定
- ⚙️ **进程配置**：进程分组、启动顺序
- 🌍 **世界配置**：游戏世界、数据库连接
- 🎮 **场景配置**：场景类型、运行模式、网络协议
- 📦 **自定义配置**：业务相关的静态数据（可选）

**配置文件位置：**
- 必须放在**直接引用了 Fantasy 框架的项目根目录**（如 `Server.Entity/`）
- Source Generator 会根据配置文件生成注册代码

**相关文件：**
- `Fantasy.config`：配置文件
- `Fantasy.xsd`：XML Schema 定义文件，提供 IDE 智能提示

---

## 完整配置示例

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

  <!-- 网络运行时配置 -->
  <network inner="TCP" maxMessageSize="1048560" />

  <!-- 会话运行时配置 -->
  <session idleTimeout="8000" idleInterval="5000" />

  <server>
    <!-- 机器配置 -->
    <machines>
      <machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />
    </machines>

    <!-- 进程配置 -->
    <processes>
      <process id="1" machineId="1" startupGroup="0" />
    </processes>

    <!-- 世界配置 -->
    <worlds>
      <world id="1" worldName="WorldA">
        <!-- 可在一个世界中配置多个数据库 -->
        <database dbType="PgSQL" dbName="master"
                  dbConnection="Host=localhost;Port=5432;Database=master;Username=postgres;Password=147369" />
        <database dbType="PgSQL" dbName="replica" dbConnection="" />
        <database dbType="MongoDB" dbName="doc" dbConnection="mongodb://localhost:27017/"/>
      </world>
      <world id="2" worldName="WorldB">
        <database dbType="MongoDB" dbName="doc" dbConnection=""/>
      </world>
    </worlds>

    <!-- 场景配置 -->
    <scenes>
      <!-- Addressable场景：寻址服务 -->
      <scene id="1001" processConfigId="1" worldConfigId="1" sceneRuntimeMode="MultiThread"
             sceneTypeString="Addressable" networkProtocol="" outerPort="0" innerPort="11001" />

      <!-- Gate场景：客户端接入网关 -->
      <scene id="1002" processConfigId="1" worldConfigId="1" sceneRuntimeMode="MultiThread"
             sceneTypeString="Gate" networkProtocol="KCP" outerPort="20000" innerPort="11002" />

      <!-- Map场景：游戏地图逻辑 -->
      <scene id="1003" processConfigId="1" worldConfigId="1" sceneRuntimeMode="MultiThread"
             sceneTypeString="Map" networkProtocol="" outerPort="0" innerPort="11003" />

      <!-- Chat场景：聊天服务 -->
      <scene id="1004" processConfigId="1" worldConfigId="1" sceneRuntimeMode="MultiThread"
             sceneTypeString="Chat" networkProtocol="" outerPort="0" innerPort="11004" />

      <!-- 跨世界场景示例 -->
      <scene id="2001" processConfigId="1" worldConfigId="2" sceneRuntimeMode="MultiThread"
             sceneTypeString="Map" networkProtocol="" outerPort="0" innerPort="11005" />
    </scenes>
  </server>
</fantasy>
```

---

## 配置参数详解

### 1. 网络配置 (`<network>`)

配置服务器间（Inner）网络通信参数。

```xml
<network inner="TCP" maxMessageSize="1048560" />
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `inner` | 枚举 | 否 | `TCP` | 服务器内部通信协议<br>• `TCP`: 可靠传输，适合大多数场景<br>• `KCP`: 低延迟UDP协议，适合对延迟敏感的场景 |
| `maxMessageSize` | 整数 | 否 | `1048560` | 单个消息体最大长度（字节）<br>默认约1.02MB，根据需要调整 |

**使用建议：**
- 常规业务推荐使用 `TCP`，稳定可靠
- 对延迟敏感的实时服务（如战斗服务器）可使用 `KCP`
- `maxMessageSize` 应根据实际业务需求调整，避免过大消息影响性能

---

### 2. 会话配置 (`<session>`)

配置客户端会话的超时检测参数。

```xml
<session idleTimeout="8000" idleInterval="5000" />
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `idleTimeout` | 整数 | 否 | `8000` | 会话空闲超时时间（毫秒）<br>超过此时间无消息交互则断开连接 |
| `idleInterval` | 整数 | 否 | `5000` | 空闲检测间隔（毫秒）<br>框架每隔此时间检查一次会话是否超时 |

**工作原理：**
- 框架每隔 `idleInterval` 毫秒检查一次所有会话
- 如果某个会话超过 `idleTimeout` 毫秒没有消息交互，则断开连接

**使用示例：**

```xml
<!-- 每5秒检测一次，超过8秒无消息则断开 -->
<session idleTimeout="8000" idleInterval="5000" />

<!-- 移动端网络不稳定，增大超时时间 -->
<session idleTimeout="15000" idleInterval="5000" />

<!-- 关闭空闲检测（不推荐） -->
<session idleTimeout="0" idleInterval="5000" />
```

**使用建议：**
- 移动端网络不稳定，适当增大 `idleTimeout`
- `idleInterval` 不宜过小，避免频繁检测影响性能
- 配合客户端心跳机制使用效果更佳

---

### 3. 机器配置 (`<machine>`)

定义物理服务器的网络地址信息。一个 `<machine>` 代表一台物理服务器或虚拟机。

```xml
<machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | 无符号整数 | ✅ | 机器唯一标识ID<br>**范围：1 ~ 4294967295** |
| `outerIP` | 字符串 | ✅ | **外网IP地址**<br>客户端连接时使用的IP（公网IP或域名） |
| `outerBindIP` | 字符串 | ✅ | **外网绑定IP**<br>服务器监听外部连接的本地IP<br>• 本地开发：`127.0.0.1`<br>• 生产环境：`0.0.0.0`（监听所有网卡）或指定网卡IP |
| `innerBindIP` | 字符串 | ✅ | **内网绑定IP**<br>服务器间通信绑定的IP<br>• 单机：`127.0.0.1`<br>• 局域网：内网IP（如 `192.168.1.10`） |

**三个IP的区别：**

| IP类型 | 用途 | 使用场景 | 示例值 |
|--------|------|----------|--------|
| `outerIP` | 客户端连接时使用的地址 | 客户端配置中的服务器地址 | 公网IP、域名、`127.0.0.1` |
| `outerBindIP` | 服务器监听外部连接的本地地址 | 服务器启动时绑定的网卡 | `0.0.0.0`、`127.0.0.1`、具体网卡IP |
| `innerBindIP` | 服务器间通信绑定的地址 | 内网服务器间通信 | `127.0.0.1`、内网IP |

**不同部署场景配置：**

| 场景 | outerIP | outerBindIP | innerBindIP |
|------|---------|-------------|-------------|
| **本地开发（单机）** | `127.0.0.1` | `127.0.0.1` | `127.0.0.1` |
| **生产环境（单机）** | 公网IP 或 域名 | `0.0.0.0` | `127.0.0.1` |
| **生产环境（多机-局域网）** | 公网IP 或 域名 | `0.0.0.0` | 内网IP（如 `192.168.1.10`） |
| **生产环境（多机-不同地域）** | 公网IP | `0.0.0.0` | 公网IP |

**配置示例：**

```xml
<!-- 本地开发 -->
<machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />

<!-- 生产环境单机 -->
<machine id="1" outerIP="game.example.com" outerBindIP="0.0.0.0" innerBindIP="127.0.0.1" />

<!-- 生产环境多机（机器1：网关服务器） -->
<machine id="1" outerIP="gate.example.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.10" />

<!-- 生产环境多机（机器2：游戏逻辑服务器） -->
<machine id="2" outerIP="logic.example.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.20" />
```

---

### 4. 进程配置 (`<process>`)

定义服务器进程的运行信息。一个 `<process>` 代表一个服务器进程实例。

```xml
<process id="1" machineId="1" startupGroup="0" />
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | 无符号整数 | ✅ | 进程唯一标识ID<br>**范围：1 ~ 4294967295** |
| `machineId` | 无符号整数 | ✅ | 所属机器ID（引用 `<machine>` 的 `id`）<br>决定进程运行在哪台机器上 |
| `startupGroup` | 无符号整数 | ✅ | 启动分组编号<br>• 相同分组的进程会同时启动<br>• 数字越小越早启动<br>• 用于控制服务启动顺序 |

**启动分组（startupGroup）说明：**

- 框架按 `startupGroup` 从小到大依次启动进程
- 同一分组的进程会并行启动
- 用于实现服务依赖关系（如先启动基础服务，再启动业务服务）

**配置示例：**

```xml
<!-- 第一批启动：基础服务（组0） -->
<process id="1" machineId="1" startupGroup="0" />  <!-- Addressable寻址服务 -->
<process id="2" machineId="1" startupGroup="0" />  <!-- Gate网关服务 -->

<!-- 第二批启动：游戏逻辑服务（组1） -->
<process id="3" machineId="1" startupGroup="1" />  <!-- Map地图服务 -->
<process id="4" machineId="1" startupGroup="1" />  <!-- Battle战斗服务 -->

<!-- 第三批启动：辅助服务（组2） -->
<process id="5" machineId="1" startupGroup="2" />  <!-- Chat聊天服务 -->
<process id="6" machineId="1" startupGroup="2" />  <!-- Log日志服务 -->
```

**使用建议：**
- 基础服务（如寻址、网关）使用较小的分组号
- 依赖基础服务的业务逻辑使用较大的分组号
- 合理规划分组可以避免启动时的依赖问题

---

### 5. 世界配置 (`<world>`)

定义游戏世界及其关联的数据库。一个 `<world>` 代表一个独立的游戏世界（如不同的游戏服务器、不同的分区）。

```xml
<world id="1" worldName="MainWorld">
    <database dbType="MongoDB" dbName="game"
              dbConnection="mongodb://localhost:27017/" />
</world>
```

#### World 参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | 无符号整数 | ✅ | 世界唯一标识ID<br>**范围：1 ~ 4294967295** |
| `worldName` | 字符串 | ✅ | 世界名称（用于标识不同的游戏世界/服务器）<br>如：`MainWorld`、`TestWorld`、`Server1` 等 |

#### Database 参数

每个 `<world>` 必须包含至少一个 `<database>` 配置。

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `dbType` | 枚举 | ✅ | 数据库类型<br>• `MongoDB` / `Mongo`: MongoDB数据库<br>• `PostgreSQL` / `PgSQL` / `Pg`: PostgreSQL数据库 |
| `dbName` | 字符串 | ✅ | 数据库名称<br>用于标识和生成数据库名称常量 |
| `dbConnection` | 字符串 | 否 | 数据库连接字符串<br>• **为空**：不连接数据库（仅生成常量）<br>• **MongoDB**：`mongodb://host:port/`<br>• **PostgreSQL**：`Host=host;Port=port;Database=db;Username=user;Password=pass` |

#### 配置示例

**示例1：单数据库配置（最简单）**

```xml
<world id="1" worldName="Game1">
    <database dbType="MongoDB" dbName="game_main"
              dbConnection="mongodb://localhost:27017/" />
</world>
```

**示例2：主从数据库配置**

```xml
<world id="1" worldName="Game1">
    <!-- 主库：游戏数据（读写） -->
    <database dbType="MongoDB" dbName="game_main"
              dbConnection="mongodb://master.db.local:27017/" />

    <!-- 从库：游戏数据（只读） -->
    <database dbType="MongoDB" dbName="game_replica"
              dbConnection="mongodb://replica.db.local:27017/" />
</world>
```

**示例3：多种数据库类型混用**

```xml
<world id="1" worldName="Game1">
    <!-- MongoDB：游戏数据 -->
    <database dbType="MongoDB" dbName="game_main"
              dbConnection="mongodb://localhost:27017/" />

    <!-- MongoDB：日志数据 -->
    <database dbType="MongoDB" dbName="game_logs"
              dbConnection="mongodb://replica.db.local:27017/" />
</world>
```

**示例4：仅生成数据库名称常量（不连接数据库）**

```xml
<world id="1" worldName="Game1">
    <!-- dbConnection 为空，框架不会连接数据库，但会生成数据库名称常量 -->
    <database dbType="MongoDB" dbName="game_main" dbConnection="" />
</world>
```

**示例5：多世界配置（多区服）**

```xml
<!-- 区服1 -->
<world id="1" worldName="Server1">
    <database dbType="MongoDB" dbName="server1_data"
              dbConnection="mongodb://db1.local:27017/" />
</world>

<!-- 区服2 -->
<world id="2" worldName="Server2">
    <database dbType="MongoDB" dbName="server2_data"
              dbConnection="mongodb://db2.local:27017/" />
</world>
```

---

### 6. 场景配置 (`<scene>`)

定义游戏场景（Scene）的运行参数，是框架的核心配置。每个 `<scene>` 代表一个独立的业务场景（如网关、地图、聊天等）。

```xml
<scene id="1001" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="MultiThread" sceneTypeString="Gate"
       networkProtocol="KCP" outerPort="20000" innerPort="11001" />
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | 无符号整数 | ✅ | 场景唯一标识ID<br>**建议使用4位数以上（如1001），避免与其他ID冲突**<br>**范围：1000 ~ 100000000** |
| `processConfigId` | 无符号整数 | ✅ | 所属进程ID（引用 `<process>` 的 `id`）<br>决定场景运行在哪个进程中 |
| `worldConfigId` | 无符号整数 | ✅ | 所属世界ID（引用 `<world>` 的 `id`）<br>决定场景属于哪个游戏世界 |
| `sceneRuntimeMode` | 枚举 | ✅ | **场景运行模式**<br>• `MainThread`: 主线程模式（单线程，适合简单场景）<br>• `MultiThread`: 多线程模式（独立线程，**推荐**）<br>• `ThreadPool`: 线程池模式（共享线程池，适合大量短任务） |
| `sceneTypeString` | 字符串 | ✅ | 场景类型名称（自定义，用于业务逻辑识别）<br>如：`Gate`、`Map`、`Chat`、`Battle`、`Login` 等 |
| `networkProtocol` | 枚举 | 否 | **网络协议类型**<br>• `TCP`: 可靠传输<br>• `KCP`: 低延迟UDP（**推荐游戏使用**）<br>• `WebSocket`: Web客户端使用<br>• `HTTP`: HTTP接口<br>• **留空**：不监听外部连接（纯内部场景） |
| `outerPort` | 无符号整数 | 否 | **外部端口**<br>客户端连接的端口号<br>• `0` 或不设置：不监听外部连接<br>• 其他：监听指定端口 |
| `innerPort` | 无符号整数 | ✅ | **内部端口**<br>服务器间通信端口<br>**每个场景必须唯一**<br>**范围：1 ~ 65535** |

#### 场景运行模式（sceneRuntimeMode）对比

| 模式 | 特点 | 适用场景 | 性能 |
|------|------|----------|------|
| `MainThread` | 运行在主线程，单线程执行 | 简单场景、调试场景 | 低 |
| `MultiThread` | **推荐**<br>独立线程，完全隔离 | 大部分业务场景 | 高 |
| `ThreadPool` | 共享线程池，多场景共享 | 大量短任务场景 | 中 |

**使用建议：**
- 大部分场景使用 `MultiThread`，性能最佳
- 轻量级场景（如日志收集）可使用 `ThreadPool`
- 避免使用 `MainThread`，除非特殊需求

#### 网络协议（networkProtocol）对比

| 协议 | 特点 | 适用场景 | 配置示例 |
|------|------|----------|----------|
| `TCP` | 可靠传输，连接稳定 | 一般游戏、HTTP服务 | `networkProtocol="TCP"` |
| `KCP` | **推荐游戏使用**<br>低延迟UDP，性能优秀 | 实时游戏、对战游戏 | `networkProtocol="KCP"` |
| `WebSocket` | Web客户端支持 | H5游戏、Web应用 | `networkProtocol="WebSocket"` |
| `HTTP` | HTTP协议 | RESTful API、后台接口 | `networkProtocol="HTTP"` |
| **留空** | 不监听外部连接 | 纯内部场景 | `networkProtocol=""` |

#### 常见场景配置示例

**示例1：Gate场景（客户端接入网关）**

```xml
<!-- Gate场景：客户端接入网关 -->
<scene id="1001" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="MultiThread" sceneTypeString="Gate"
       networkProtocol="KCP" outerPort="20000" innerPort="11001" />
```

**特点：**
- 使用 KCP 协议（低延迟）
- 监听外部端口 20000（客户端连接）
- 内部端口 11001（服务器间通信）

**示例2：Map场景（游戏地图逻辑，纯内部）**

```xml
<!-- Map场景：游戏地图逻辑（纯内部，不对外） -->
<scene id="1002" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="MultiThread" sceneTypeString="Map"
       networkProtocol="" outerPort="0" innerPort="11002" />
```

**特点：**
- 不监听外部端口（`networkProtocol=""` 和 `outerPort="0"`）
- 仅用于服务器间通信（通过 `innerPort`）

**示例3：Chat场景（聊天服务）**

```xml
<!-- Chat场景：聊天服务（纯内部） -->
<scene id="1003" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="ThreadPool" sceneTypeString="Chat"
       networkProtocol="" outerPort="0" innerPort="11003" />
```

**特点：**
- 使用线程池模式（聊天消息处理量大，任务短）
- 不对外监听（通过其他场景转发消息）

**示例4：HTTP API场景（对外接口）**

```xml
<!-- HTTP场景：对外API接口 -->
<scene id="1004" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="MultiThread" sceneTypeString="API"
       networkProtocol="HTTP" outerPort="8080" innerPort="11004" />
```

**特点：**
- 使用 HTTP 协议
- 监听 8080 端口（对外提供 RESTful API）

**示例5：Battle场景（战斗服务）**

```xml
<!-- Battle场景：实时战斗服务 -->
<scene id="1005" processConfigId="1" worldConfigId="1"
       sceneRuntimeMode="MultiThread" sceneTypeString="Battle"
       networkProtocol="" outerPort="0" innerPort="11005" />
```

**特点：**
- 高性能多线程模式
- 纯内部场景（通过 Gate 转发战斗消息）

---

## 配置最佳实践

### 1. 端口规划建议

良好的端口规划可以避免冲突，便于管理。

```xml
<!-- 外部端口（客户端连接）：20000+ -->
<scene outerPort="20000" ... />  <!-- Gate场景 -->
<scene outerPort="20001" ... />  <!-- 备用Gate场景 -->
<scene outerPort="8080" ... />   <!-- HTTP API -->

<!-- 内部端口（服务器间）：11000+ -->
<scene innerPort="11001" ... />  <!-- 场景1 -->
<scene innerPort="11002" ... />  <!-- 场景2 -->
<scene innerPort="11003" ... />  <!-- 场景3 -->
```

**端口规划原则：**
- ✅ 外部端口和内部端口使用不同范围，避免冲突
- ✅ 内部端口必须每个场景唯一
- ✅ 生产环境避免使用 1024 以下的系统端口
- ✅ 建议内部端口从 11000 开始，外部端口从 20000 开始
- ✅ 留出足够的端口范围，方便后续扩展

---

### 2. 场景ID规划建议

使用规范的场景ID可以提高可读性和可维护性。

```xml
<!-- 使用4位数，按类型分段 -->
<scene id="1001" sceneTypeString="Gate" ... />       <!-- 1000-1999: 网关 -->
<scene id="1002" sceneTypeString="Gate" ... />       <!-- 多个网关 -->

<scene id="2001" sceneTypeString="Map" ... />        <!-- 2000-2999: 地图 -->
<scene id="2002" sceneTypeString="Map" ... />        <!-- 多个地图 -->

<scene id="3001" sceneTypeString="Chat" ... />       <!-- 3000-3999: 聊天 -->

<scene id="4001" sceneTypeString="Battle" ... />     <!-- 4000-4999: 战斗 -->
<scene id="4002" sceneTypeString="Battle" ... />     <!-- 多个战斗场景 -->

<scene id="8001" sceneTypeString="API" ... />        <!-- 8000-8999: HTTP API -->
```

**场景ID规划原则：**
- ✅ 使用 4 位数以上（1000+），避免与其他ID冲突
- ✅ 按场景类型分段，便于识别（如 1000-1999 为网关，2000-2999 为地图）
- ✅ 同类型场景使用连续ID
- ✅ 预留足够的ID范围，方便后续扩展

---

### 3. 多机部署配置示例

#### 场景1：单机部署（最简单）

```xml
<machines>
    <machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />
</machines>

<processes>
    <process id="1" machineId="1" startupGroup="0" />
</processes>

<scenes>
    <scene id="1001" processConfigId="1" ... />
    <scene id="1002" processConfigId="1" ... />
</scenes>
```

#### 场景2：多机部署（局域网）

```xml
<!-- 机器1：网关服务器 -->
<machine id="1" outerIP="gate.game.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.10" />
<process id="1" machineId="1" startupGroup="0" />
<scene id="1001" processConfigId="1" sceneTypeString="Gate" networkProtocol="KCP" outerPort="20000" innerPort="11001" ... />

<!-- 机器2：游戏逻辑服务器 -->
<machine id="2" outerIP="logic.game.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.20" />
<process id="2" machineId="2" startupGroup="1" />
<scene id="2001" processConfigId="2" sceneTypeString="Map" networkProtocol="" outerPort="0" innerPort="11002" ... />
<scene id="2002" processConfigId="2" sceneTypeString="Battle" networkProtocol="" outerPort="0" innerPort="11003" ... />

<!-- 机器3：数据库服务器 -->
<machine id="3" outerIP="db.game.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.30" />
<process id="3" machineId="3" startupGroup="0" />
<scene id="3001" processConfigId="3" sceneTypeString="DataService" networkProtocol="" outerPort="0" innerPort="11004" ... />
```

#### 场景3：分布式部署（跨地域）

```xml
<!-- 华东区-网关服务器 -->
<machine id="1" outerIP="gate-east.game.com" outerBindIP="0.0.0.0" innerBindIP="公网IP1" />
<process id="1" machineId="1" startupGroup="0" />
<scene id="1001" processConfigId="1" sceneTypeString="Gate" ... />

<!-- 华南区-网关服务器 -->
<machine id="2" outerIP="gate-south.game.com" outerBindIP="0.0.0.0" innerBindIP="公网IP2" />
<process id="2" machineId="2" startupGroup="0" />
<scene id="1002" processConfigId="2" sceneTypeString="Gate" ... />

<!-- 中央-游戏逻辑服务器 -->
<machine id="3" outerIP="logic.game.com" outerBindIP="0.0.0.0" innerBindIP="公网IP3" />
<process id="3" machineId="3" startupGroup="1" />
<scene id="2001" processConfigId="3" sceneTypeString="Map" ... />
```

---

### 4. 开发环境简化配置

本地开发时的最小配置，快速上手：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

    <!-- 网络配置：默认值 -->
    <network inner="TCP" maxMessageSize="1048560" />

    <!-- 会话配置：默认值 -->
    <session idleTimeout="8000" idleInterval="5000" />

    <server>
        <!-- 机器配置：本地 -->
        <machines>
            <machine id="1" outerIP="127.0.0.1" outerBindIP="127.0.0.1" innerBindIP="127.0.0.1" />
        </machines>

        <!-- 进程配置：单进程 -->
        <processes>
            <process id="1" machineId="1" startupGroup="0" />
        </processes>

        <!-- 世界配置：开发环境（不连接数据库） -->
        <worlds>
            <world id="1" worldName="Dev">
                <database duty="0" dbType="MongoDB" dbName="dev" dbConnection="" />
            </world>
        </worlds>

        <!-- 场景配置：只有Gate场景 -->
        <scenes>
            <scene id="1001" processConfigId="1" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="Gate"
                   networkProtocol="KCP" outerPort="20000" innerPort="11001" sceneType="1" />
        </scenes>
    </server>
</fantasy>
```

**特点：**
- ✅ 单机单进程配置，最简单
- ✅ 不连接数据库（`dbConnection=""`）
- ✅ 只有一个Gate场景，快速测试客户端连接

---

### 5. 生产环境完整配置

生产环境的完整配置示例，包含所有常见场景：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

    <!-- 网络配置 -->
    <network inner="TCP" maxMessageSize="2097120" />  <!-- 2MB -->

    <!-- 会话配置 -->
    <session idleTimeout="15000" idleInterval="5000" />  <!-- 15秒超时 -->

    <server>
        <!-- 机器配置 -->
        <machines>
            <machine id="1" outerIP="gate.game.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.10" />
            <machine id="2" outerIP="logic.game.com" outerBindIP="0.0.0.0" innerBindIP="192.168.1.20" />
        </machines>

        <!-- 进程配置 -->
        <processes>
            <process id="1" machineId="1" startupGroup="0" />  <!-- 网关进程 -->
            <process id="2" machineId="2" startupGroup="1" />  <!-- 逻辑进程 -->
        </processes>

        <!-- 世界配置 -->
        <worlds>
            <world id="1" worldName="Server1">
                <!-- 主库 -->
                <database duty="0" dbType="MongoDB" dbName="game_main"
                          dbConnection="mongodb://db.local:27017/" />
                <!-- 从库 -->
                <database duty="1" dbType="MongoDB" dbName="game_replica"
                          dbConnection="mongodb://db-replica.local:27017/" />
                <!-- 日志库 -->
                <database duty="2" dbType="PgSQL" dbName="game_logs"
                          dbConnection="Host=db-log.local;Port=5432;Database=logs;Username=user;Password=pass" />
            </world>
        </worlds>

        <!-- 场景配置 -->
        <scenes>
            <!-- 网关场景 -->
            <scene id="1001" processConfigId="1" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="Gate"
                   networkProtocol="KCP" outerPort="20000" innerPort="11001" sceneType="1" />

            <!-- 寻址场景 -->
            <scene id="1002" processConfigId="2" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="Addressable"
                   networkProtocol="" outerPort="0" innerPort="11002" sceneType="2" />

            <!-- 地图场景 -->
            <scene id="2001" processConfigId="2" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="Map"
                   networkProtocol="" outerPort="0" innerPort="11003" sceneType="3" />

            <!-- 聊天场景 -->
            <scene id="3001" processConfigId="2" worldConfigId="1"
                   sceneRuntimeMode="ThreadPool" sceneTypeString="Chat"
                   networkProtocol="" outerPort="0" innerPort="11004" sceneType="4" />

            <!-- HTTP API场景 -->
            <scene id="8001" processConfigId="1" worldConfigId="1"
                   sceneRuntimeMode="MultiThread" sceneTypeString="API"
                   networkProtocol="HTTP" outerPort="8080" innerPort="11005" sceneType="5" />
        </scenes>
    </server>
</fantasy>
```

---

## 配置验证

Fantasy Framework 提供了完善的配置验证功能，在服务器启动时会自动检查配置文件的正确性。

### 自动验证项

框架启动时会自动验证以下内容：

| 验证项 | 说明 | 错误示例 |
|--------|------|----------|
| ✅ **ID唯一性** | 机器ID、进程ID、世界ID、场景ID不能重复 | 两个场景都使用了 `id="1001"` |
| ✅ **引用完整性** | `machineId`、`processConfigId`、`worldConfigId` 必须引用已存在的配置 | `machineId="999"` 但没有定义 `<machine id="999">` |
| ✅ **端口冲突** | 同一机器上的 `outerPort` 和 `innerPort` 不能冲突 | 两个场景在同一机器上都使用了 `innerPort="11001"` |
| ✅ **场景ID范围** | 场景ID必须在合理范围内（1000 ~ 100000000） | `<scene id="1">` 或 `<scene id="999999999">` |
| ✅ **数据库配置** | 数据库连接字符串格式正确（如果配置了连接字符串） | MongoDB连接字符串格式错误 |
| ✅ **必填参数** | 所有必填参数都已配置 | 缺少 `<machine>` 的 `outerIP` 参数 |
| ✅ **枚举值** | 枚举类型的值必须在允许范围内 | `networkProtocol="ABC"` |

### 验证失败处理

**如果配置有误，框架会：**
1. 在控制台输出详细的错误信息
2. 指出具体的错误位置和原因
3. 停止启动，退出程序

**错误信息示例：**

```
[ERROR] Fantasy.config 验证失败：
- 场景 ID 1001 和 1002 的 innerPort 冲突：都使用了 11001
- 进程 ID 2 的 machineId=999 引用了不存在的机器
- 场景 ID 50 的 ID 过小，建议使用 1000 以上的ID
```

---

## 常见问题

### Q1：配置文件应该放在哪里？

**A：** 必须放在**直接引用了 Fantasy 框架的项目根目录**（如 `Server.Entity/`）。

**原因：**
- Source Generator 会根据配置文件生成注册代码
- 生成的代码必须在引用了 Fantasy 的项目中
- 其他项目通过引用该项目自动获得生成的代码

### Q2：如何在代码中获取配置信息？

**A：** 通过 Scene 实例获取：

```csharp
// 获取场景配置
var sceneConfig = scene.SceneConfig;
Console.WriteLine($"场景ID: {sceneConfig.Id}");
Console.WriteLine($"场景类型: {sceneConfig.SceneTypeString}");

// 获取世界配置
var worldConfig = scene.World.WorldConfig;
Console.WriteLine($"世界名称: {worldConfig.WorldName}");

```

### Q3：可以动态修改配置吗？

**A：** **不可以**。`Fantasy.config` 是启动配置，在服务器启动时读取一次。

**如果需要动态配置：**
- 使用 Excel 配置表（通过 ConfigTable 工具导出）
- 使用数据库存储动态配置
- 使用配置管理系统（如 Apollo、Nacos）

### Q4：如何配置多个网关场景？

**A：** 配置多个 `<scene>`，使用不同的 `id` 和 `outerPort`：

```xml
<!-- 网关1 -->
<scene id="1001" ... outerPort="20000" innerPort="11001" sceneTypeString="Gate" />

<!-- 网关2 -->
<scene id="1002" ... outerPort="20001" innerPort="11002" sceneTypeString="Gate" />
```

### Q5：database 的 dbConnection 为空时有什么用？

**A：** 即使 `dbConnection` 为空，框架也会生成数据库名称常量，方便代码中使用：

```csharp
// 即使不连接数据库，也可以使用生成的常量
var dbName = DatabaseName.game_main;  // "game_main"
```

---

## 总结

本文档详细介绍了 `Fantasy.config` 的所有配置参数。核心要点：

1. **网络配置**：配置内部通信协议和消息大小限制
2. **会话配置**：配置客户端超时检测
3. **机器配置**：定义物理服务器的IP地址
4. **进程配置**：定义进程运行信息和启动顺序
5. **世界配置**：定义游戏世界和数据库连接
6. **场景配置**：定义业务场景的运行参数（核心）
7. **单位配置**：自定义业务数据（可选）

**下一步：**
- 💻 阅读 [配置系统使用指南](02-ConfigUsage.md) 学习如何在代码中使用配置
- 🎮 阅读 [ECS 系统](04-ECS.md) 学习实体组件系统（待完善）
- 🌐 阅读 [网络开发](06-Network.md) 学习消息处理（待完善）
- 📚 查看 `Examples/Config` 目录下的完整示例

---
