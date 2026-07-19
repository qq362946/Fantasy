# Fantasy.config 配置文件详解

`Fantasy.config` 是 Fantasy 服务器的启动配置文件。它负责配置 ID 生成策略、内部网络、Session，以及服务器拓扑的来源。

服务器拓扑支持两种模式：

| 模式 | 配置来源 | SceneType 来源 | 适用场景 |
|---|---|---|---|
| 本地模式 | `<server>` | `<server><scenes>` | 本地开发、单机部署、静态拓扑 |
| Control Center 模式 | Fantasy Control Center | `<controlCenter><sceneTypes>` | 多机部署、服务发现、动态管理拓扑 |

无论使用哪种模式，`idFactory`、`network` 和 `session` 始终从本地 `Fantasy.config` 加载。

## 文件位置

`Fantasy.config` 必须放在直接引用 Fantasy.Net 的项目根目录，并作为 `AdditionalFiles` 交给源生成器：

```xml
<ItemGroup>
  <None Update="Fantasy.config">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <AdditionalFiles Include="Fantasy.config" />
</ItemGroup>
```

建议同时保留同目录下的 `Fantasy.xsd`，用于 XML 结构校验和 IDE 智能提示。

## 节点顺序

根节点中的配置必须按照下面的顺序排列：

```xml
<fantasy>
  <idFactory />
  <network />
  <session />
  <controlCenter />
  <server />
</fantasy>
```

`idFactory`、`network`、`session`、`controlCenter` 可以省略，`server` 必须存在。启用 Control Center 且不需要本地回退时，可以使用空的 `<server />`。

## 本地模式完整示例

没有配置 `<controlCenter>`，或者它的 `enabled="false"` 时，框架完全使用 `<server>` 中的本地拓扑。

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

  <idFactory type="Default" />
  <network inner="TCP" maxMessageSize="1048560" />
  <session idleTimeout="8000" idleInterval="5000" />

  <server>
    <machines>
      <machine id="1"
               outerIP="127.0.0.1"
               outerBindIP="127.0.0.1"
               innerBindIP="127.0.0.1" />
    </machines>

    <processes>
      <process id="1" machineId="1" startupGroup="0" />
    </processes>

    <worlds>
      <world id="1" worldName="WorldA">
        <database dbType="MongoDB"
                  dbName="fantasy_main"
                  dbConnection="mongodb://127.0.0.1:27017/"
                  isDefault="true" />
        <database dbType="MongoDB"
                  dbName="fantasy_log"
                  dbConnection="" />
      </world>
    </worlds>

    <scenes>
      <scene id="1001"
             processConfigId="1"
             worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Gate"
             networkProtocol="KCP"
             outerPort="20000"
             innerPort="11001" />

      <scene id="1002"
             processConfigId="1"
             worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Map"
             networkProtocol=""
             outerPort="0"
             innerPort="11002" />
    </scenes>
  </server>
</fantasy>
```

## Control Center 模式完整示例

启用 Control Center 后，Machine、Process、World、Database 和 Scene 从 Control Center 获取。下面的 `<server>` 只有在 `fallbackToLocal="true"` 且远程配置加载失败时才生效。

```xml
<?xml version="1.0" encoding="utf-8" ?>
<fantasy xmlns="http://fantasy.net/config"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://fantasy.net/config Fantasy.xsd">

  <idFactory type="Default" />
  <network inner="TCP" maxMessageSize="1048560" />
  <session idleTimeout="8000" idleInterval="5000" />

  <controlCenter enabled="true"
                 url="http://127.0.0.1:5000"
                 fallbackToLocal="true"
                 heartbeatIntervalSeconds="5"
                 leaseSeconds="15">
    <sceneTypes>
      <sceneType id="1" name="Gate" />
      <sceneType id="2" name="Map" />
      <sceneType id="3" name="HttpGift" />
    </sceneTypes>
  </controlCenter>

  <server>
    <!-- Control Center 不可用时使用的本地兜底拓扑 -->
    <machines>
      <machine id="1"
               outerIP="127.0.0.1"
               outerBindIP="127.0.0.1"
               innerBindIP="127.0.0.1" />
    </machines>

    <processes>
      <process id="1"
               namespaceId="1"
               machineId="1"
               startupGroup="0" />
    </processes>

    <worlds>
      <world id="1"
             namespaceId="1"
             groupId="1"
             worldName="WorldA">
        <database dbType="MongoDB"
                  dbName="fantasy_main"
                  dbConnection=""
                  isDefault="true" />
      </world>
    </worlds>

    <scenes>
      <scene id="1001"
             processConfigId="1"
             worldConfigId="1"
             sceneRuntimeMode="MultiThread"
             sceneTypeString="Gate"
             networkProtocol="KCP"
             outerPort="20000"
             innerPort="11001" />
    </scenes>
  </server>
</fantasy>
```

如果明确不允许本地回退，可以简化为：

```xml
<controlCenter enabled="true"
               url="https://control.example.com"
               fallbackToLocal="false">
  <sceneTypes>
    <sceneType id="1" name="Gate" />
    <sceneType id="2" name="Map" />
  </sceneTypes>
</controlCenter>
<server />
```

## 配置参数

### 1. ID 生成策略 (`<idFactory>`)

```xml
<idFactory type="Default" />
```

| 参数 | 必填 | 默认值 | 说明 |
|---|---|---|---|
| `type` | 否 | `Default` | `Default` 或 `World`；`None` 是保留值，不用于正常服务器运行 |

#### Default

- Scene ID 范围为 `1 ~ 65535`。
- 适合大多数项目。
- 不在生成的 EntityId 和 RuntimeId 中保存 World ID。

#### World

- 生成的 ID 中包含 World ID，适合需要合区并规避 ID 冲突的项目。
- World ID 范围为 `1 ~ 255`。
- 每个 World 的 Scene ID 必须位于 `worldId * 1000 + 1` 到 `worldId * 1000 + 255`。

示例：World ID 为 `2` 时，Scene ID 必须位于 `2001 ~ 2255`。

### 2. 网络配置 (`<network>`)

```xml
<network inner="TCP" maxMessageSize="1048560" />
```

| 参数 | 必填 | 默认值 | 说明 |
|---|---|---|---|
| `inner` | 否 | `TCP` | 服务器内部通信协议，可选 `TCP`、`KCP` |
| `maxMessageSize` | 否 | `1048560` | 单条消息允许的最大字节数，约 1 MB |

所有互相通信的进程应使用一致的 `maxMessageSize`。不要无上限放大该值，避免超大消息增加内存和网络压力。

### 3. Session 配置 (`<session>`)

```xml
<session idleTimeout="8000" idleInterval="5000" />
```

| 参数 | 必填 | 默认值 | 单位 | 说明 |
|---|---|---|---|---|
| `idleTimeout` | 否 | `8000` | 毫秒 | Session 超过该时间没有消息后断开 |
| `idleInterval` | 否 | `5000` | 毫秒 | Session 空闲检测间隔 |

`idleInterval` 不宜设置得过小，否则会增加空闲扫描频率。

### 4. Control Center (`<controlCenter>`)

```xml
<controlCenter enabled="true"
               url="http://127.0.0.1:5000"
               fallbackToLocal="true"
               heartbeatIntervalSeconds="5"
               leaseSeconds="15">
  <sceneTypes>
    <sceneType id="1" name="Gate" />
    <sceneType id="2" name="Map" />
  </sceneTypes>
</controlCenter>
```

| 参数 | 必填 | 默认值 | 说明 |
|---|---|---|---|
| `enabled` | 否 | `false` | 是否启用 Control Center 和服务发现 |
| `url` | 是 | 无 | Control Center HTTP/HTTPS 根地址，不要包含 `/api/v1` |
| `fallbackToLocal` | 否 | `true` | 远程配置加载失败时是否使用本地 `<server>` |
| `heartbeatIntervalSeconds` | 否 | `5` | 批量心跳间隔，必须大于 `0` 且小于 `leaseSeconds`；当前也作为本地发现缓存时间 |
| `leaseSeconds` | 否 | `15` | 服务实例租约时长，范围 `5 ~ 300` 秒 |

建议让 `leaseSeconds` 至少达到 `heartbeatIntervalSeconds` 的 3 倍，为短暂网络抖动预留恢复时间。

#### 加载流程

1. 框架始终先读取本地 `idFactory`、`network` 和 `session`。
2. `enabled="true"` 时连接 Control Center 并获取当前进程需要的运行时配置。
3. 加载成功后，`<server>` 中的本地拓扑不参与运行。
4. 加载失败且 `fallbackToLocal="true"` 时，使用本地 `<server>`。
5. 加载失败且 `fallbackToLocal="false"` 时，服务器启动失败。

Control Center 模式下，Scene 启动后会注册服务实例，并由同一 OS 进程统一发送批量心跳。正常关闭时实例会主动下线；异常退出后由租约自动失效。

### 5. SceneType 声明 (`<sceneTypes>`)

`sceneTypes` 只在 `controlCenter.enabled="true"` 时使用，并且此时必须至少配置一项。

```xml
<sceneTypes>
  <sceneType id="1" name="Gate" />
  <sceneType id="2" name="Map" />
  <sceneType id="3" name="HttpGift" />
</sceneTypes>
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 大于 `0` 的稳定数字 ID，不能重复 |
| `name` | 是 | SceneType 名称，必须匹配 `[A-Z][A-Za-z0-9_]*`，不能重复 |

源生成器会据此生成：

```csharp
SceneType.Gate
SceneType.Map
SceneType.HttpGift
```

注意事项：

- `id` 发布后不要修改，也不要把删除类型的 ID 分配给其他类型。
- Control Center 中的 `SceneType` 必须与这里的 `name` 完全一致，包括大小写。
- 新增同类型的 Scene 实例不需要修改 `sceneTypes`，也不需要重新编译。
- 增加全新的 Scene 类型时，必须在这里声明并重新编译。
- 允许本地回退时，本地 `<scenes>` 使用的所有 `sceneTypeString` 都必须在这里声明。

未启用 Control Center 时不需要 `sceneTypes`。源生成器会按照本地 `<scenes>` 中 SceneType 第一次出现的顺序，从 `1` 开始生成常量 ID。

### 6. 本地服务器配置 (`<server>`)

`server` 包含四部分：

```xml
<server>
  <machines>...</machines>
  <processes>...</processes>
  <worlds>...</worlds>
  <scenes>...</scenes>
</server>
```

| 运行方式 | `<server>` 的作用 |
|---|---|
| 未启用 Control Center | 唯一的服务器拓扑来源 |
| Control Center 加载成功 | 不加载本地拓扑 |
| Control Center 失败且允许回退 | 作为完整兜底拓扑 |
| Control Center 失败且禁止回退 | 不使用，服务器启动失败 |

### 7. Machine (`<machine>`)

```xml
<machine id="1"
         outerIP="game.example.com"
         outerBindIP="0.0.0.0"
         innerBindIP="192.168.1.10" />
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 大于 `0` 的唯一 Machine ID |
| `outerIP` | 是 | 对外公布的 IP 或域名，供客户端连接 |
| `outerBindIP` | 是 | 外网监听绑定地址；TCP/KCP 可使用 IP 或解析到本机网卡的域名 |
| `innerBindIP` | 是 | 内网监听和服务器间通信地址；可使用 IP 或解析到本机网卡的域名，并且必须能被其他服务器访问 |

不要把多机部署的 `innerBindIP` 配置成 `127.0.0.1`。Control Center 部署在外网时，注册的 Host 和端口也必须能被调用方实际访问。

Kubernetes 中推荐使用 StatefulSet Pod 专属 DNS，例如 `fantasy-gate-0.fantasy-gate-headless.game.svc.cluster.local`。不要使用普通 ClusterIP Service DNS：它解析到的虚拟 IP 不属于 Pod 本地网卡；也不要使用会返回多个 Pod 的共享 Headless Service 名称作为 `innerBindIP`。完整配置见 [Kubernetes 部署指南](../04-Advanced/14-Deployment.md)。

### 8. Process (`<process>`)

```xml
<process id="1"
         namespaceId="1"
         machineId="1"
         startupGroup="0" />
```

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 大于 `0` 的唯一 Process ID |
| `namespaceId` | 条件必填 | 纯本地模式可省略；Control Center 本地回退时必须大于 `0` |
| `machineId` | 是 | 必须引用已存在的 Machine ID |
| `startupGroup` | 是 | 启动分组；`0` 表示不按分组筛选 |

同一个 OS 进程内同时启动的 Fantasy Process 必须属于同一个 Namespace。

### 9. World 与 Database

```xml
<world id="1"
       namespaceId="1"
       groupId="1"
       worldName="WorldA">
  <database dbType="MongoDB"
            dbName="fantasy_main"
            dbConnection="mongodb://127.0.0.1:27017/"
            isDefault="true" />
  <database dbType="PgSQL"
            dbName="fantasy_log"
            dbConnection="" />
</world>
```

#### World 参数

| 参数 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 大于 `0` 的唯一 World ID；`idFactory="World"` 时不能超过 `255` |
| `namespaceId` | 条件必填 | 纯本地模式可省略；Control Center 本地回退时必须大于 `0` |
| `groupId` | 条件必填 | 纯本地模式可省略；Control Center 本地回退时必须大于 `0` |
| `worldName` | 是 | World 名称 |

`namespaceId` 和 `groupId` 必须同时省略（解析为 `0`），或者同时大于 `0`。本地回退时，Scene 引用的 Process 和 World 必须属于同一个 Namespace。

Fantasy.config 中不会定义 Namespace 和 WorldGroup 列表。它们由 Control Center 管理；本地回退配置只保存对应的 `namespaceId` 和 `groupId`。

#### Database 参数

每个 World 至少包含一个 `database`：

| 参数 | 必填 | 默认值 | 说明 |
|---|---|---|---|
| `dbType` | 是 | 无 | `MongoDB`、`Mongo`、`PostgreSQL`、`Postgres`、`PgSQL`、`Pg`、`PG` |
| `dbName` | 是 | 无 | 同一 World 内唯一的数据库名称 |
| `dbConnection` | 否 | 空字符串 | 数据库连接字符串；为空时保留数据库名称但不建立连接 |
| `isDefault` | 否 | `false` | 是否为该 World 的默认数据库 |

一个 World 最多应标记一个默认数据库。没有设置 `isDefault="true"` 时，框架使用第一条数据库配置作为默认数据库。

### 10. Scene (`<scene>`)

```xml
<scene id="1001"
       processConfigId="1"
       worldConfigId="1"
       sceneRuntimeMode="MultiThread"
       sceneTypeString="Gate"
       networkProtocol="KCP"
       outerPort="20000"
       innerPort="11001" />
```

| 参数 | 必填 | 默认值 | 说明 |
|---|---|---|---|
| `id` | 是 | 无 | 大于 `0` 的唯一 Scene ID，并满足当前 `idFactory` 的范围要求 |
| `processConfigId` | 是 | 无 | 必须引用已存在的 Process ID |
| `worldConfigId` | 是 | 无 | 必须引用已存在的 World ID |
| `sceneRuntimeMode` | 是 | 无 | `MainThread`、`MultiThread` 或 `ThreadPool` |
| `sceneTypeString` | 是 | 无 | Scene 类型名称，必须存在于源生成器生成的 SceneType 字典中 |
| `networkProtocol` | 否 | 空 | `TCP`、`KCP`、`WebSocket`、`HTTP`；纯内部 Scene 留空 |
| `outerPort` | 否 | `0` | 外网监听端口，范围 `0 ~ 65535`；`0` 表示不监听外网 |
| `innerPort` | 是 | 无 | 内网监听端口，范围 `1 ~ 65535` |

规则：

- `outerPort > 0` 时必须配置 `networkProtocol`。
- 同一个 Scene 的 `innerPort` 和 `outerPort` 不能相同。
- 同一台 Machine 上，所有 Scene 使用的内外网端口不能冲突。
- Scene 引用的 Process 和 World 必须属于同一个 Namespace。
- 大多数业务 Scene 推荐使用 `MultiThread`；大量轻量 Scene 才考虑 `ThreadPool`。

## Source Generator 行为

### 本地模式

源生成器从 `<server><scenes>` 收集不同的 `sceneTypeString`，按照第一次出现的顺序从 `1` 开始生成 `SceneType` 常量。

例如：

```xml
<scene sceneTypeString="Gate" ... />
<scene sceneTypeString="Map" ... />
<scene sceneTypeString="Gate" ... />
```

生成：

```csharp
public static class SceneType
{
    public const int Gate = 1;
    public const int Map = 2;
}
```

调整 SceneType 第一次出现的顺序会改变生成的数字 ID。已经依赖固定 ID 的项目不要随意调整顺序。

### Control Center 模式

源生成器只读取 `<controlCenter><sceneTypes>`，不会根据 Control Center 中当前存在多少个 Scene 实例生成代码。这让开发者可以动态增加已有类型的实例，同时保持 SceneType ID 稳定。

## 配置验证

配置会经过 XSD、源生成器和运行时统一校验，主要包括：

- Machine、Process、World、Scene ID 大于 `0` 且各自唯一。
- Process 引用的 Machine 必须存在。
- Scene 引用的 Process 和 World 必须存在。
- World 的 Namespace 和 WorldGroup 配置必须成对出现。
- Scene 引用的 Process 和 World 必须属于同一个 Namespace。
- SceneType 必须已经声明。
- Scene 端口范围正确，并且同一 Machine 上没有冲突。
- `idFactory="World"` 时 World ID 和 Scene ID 满足对应范围。
- Control Center 心跳间隔和租约范围正确。
- 本地回退使用的 SceneType 都存在于 `controlCenter.sceneTypes`。

建议修改配置后执行：

```bash
xmllint --noout --schema Fantasy.xsd Fantasy.config
dotnet build
```

`xmllint` 检查 XML 结构，`dotnet build` 会运行源生成器检查 SceneType 规则；服务器启动时还会执行引用关系和端口冲突校验。

## 常见问题

### 启用 Control Center 后，`<server><scenes>` 是否还生效？

Control Center 配置加载成功时不生效。只有 `fallbackToLocal="true"` 且加载失败时，完整的本地 `<server>` 才会作为兜底。

### 为什么启用 Control Center 后还需要 `sceneTypes`？

`SceneType` 是编译期常量，而 Control Center 中的 Scene 实例是运行时数据。显式声明 `sceneTypes` 可以让常量稳定，同时允许动态增加已有类型的实例。

### 新增服务后是否需要重新编译？

- 新增已有 SceneType 的实例：不需要重新编译。
- 新增全新 SceneType：需要增加一个 `sceneType` 声明并重新编译。

### `dbConnection` 可以为空吗？

可以。框架会保留数据库名称配置，但不会建立数据库连接。

### 配置可以动态修改吗？

- 本地 `<server>` 配置只在启动时读取，修改后需要重启。
- Control Center 中的拓扑可以动态管理并持久化。
- 服务注册、心跳、下线和发现是动态的。
- 修改某个 Process 需要启动的 Scene 后，需要重启对应 Process 才能按照新配置创建 Scene。

## 相关文档

- [服务发现使用指南](../04-Advanced/NetworkDevelopment/11-ServiceDiscovery.md)
- [配置系统使用指南](05-ConfigUsage.md)
- [命令行参数](03-CommandLineArguments.md)
- [服务器启动代码](02-WritingStartupCode.md)
