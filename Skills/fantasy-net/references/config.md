# Fantasy.config 入口

Fantasy.config 是框架的 XML 启服配置文件，位于直接引用 Fantasy.Net 包的项目根目录下，与该项目的 `.csproj` 同级。它既可以保存完整本地拓扑，也可以只负责启用 Control Center，由 Control Center 提供运行时拓扑。

**完整注释模板见 `templates/Fantasy.config`。** 需要直接落 XML 时再读模板，不要一开始就整份吞进去。

## 先确定配置来源

```text
没有启用 controlCenter
  -> 从 <server> 加载 Machine / Process / World / Database / Scene

controlCenter.enabled="true" 且远程加载成功
  -> 从 Control Center 加载运行时拓扑，<server> 不参与本次启动

Control Center 加载失败且 fallbackToLocal="true"
  -> 回退到 <server>，本地配置必须完整并带隔离 ID
```

服务发现的接入和业务 API 见 `service-discovery/index.md`。

## 配置里的 Scene 和 ECS 里的 Scene 是同一个概念

本地模式下，`Fantasy.config` 里的每一条 `<scene>` 配置都会创建一个对应的 **Root Scene**。Control Center 模式下，同样的运行时 Scene 定义改由 Control Center 提供。

也就是说：

- 本地 `<scene ... sceneTypeString="Gate" ... />` 或 Control Center 中的 Gate Scene 都不是纯展示项，而是会实际运行一个 `SceneType.Gate` 的 Scene
- 每新增一个分配给目标 Process 的 Scene，通常就意味着启动该 Process 时会多创建一个运行中 Scene
- 这些运行中的 Root Scene，就是 `references/ecs/scene.md` 里说的 ECS 顶层容器

可以这样理解：

```text
本地 <scene> 或 Control Center Scene 定义
            ↓
服务器启动时创建对应的 Root Scene
            ↓
该 Scene 再承载自己的 Entity / Component / Timer / Event / Network 等运行时资源
```

所以当用户说“我要加一个 Map Scene”时，实际是在做两件相关的事：

1. 在当前配置来源中增加一条 Scene：本地模式修改 `<scene>`，Control Center 模式在后台新增 Scene
2. 在代码里为这个 `SceneType` 准备对应的初始化逻辑，例如 `OnCreateScene` 中添加组件

## Workflow

```text
新建项目或从零写配置 -> config-scenarios.md
启用 Control Center 或服务发现 -> service-discovery/index.md
新增 / 修改 machine 或 process -> templates/Fantasy.config
新增 / 修改 world 或 database -> config-scenarios.md
新增 / 删除 scene、改端口、改网络协议 -> config-scenarios.md
需要查节点属性和默认值 -> templates/Fantasy.config
```

## 必记规则

1. 现在禁止使用 `<configTable>`，统一使用 `<server>` 方式
2. `machine` / `process` / `world` / `scene` 的 `id` 不能为 0，且同类型内不能重复
3. `scene.outerPort > 0` 时必须填写 `networkProtocol`
4. 同一机器上所有 `innerPort` 和 `outerPort` 不能冲突
5. `idFactory type="World"` 时，`worldId` 和 `sceneId` 范围要符合约束
6. `controlCenter.enabled="true"` 时必须声明稳定且唯一的 `<sceneTypes>`
7. `fallbackToLocal="true"` 时，本地 Process 和 World 必须配置正确的 Namespace / WorldGroup 隔离 ID

## 核心节点速查

- `<idFactory>` - ID 生成策略，常用 `Default` 或 `World`
- `<network>` - 内网协议与消息大小
- `<session>` - 空闲检测超时配置
- `<controlCenter>` - Control Center 地址、回退策略、心跳与租约
- `<controlCenter><sceneTypes>` - 服务发现模式下由源生成器生成的编译期 SceneType 声明
- `<server>` - 机器、进程、World、数据库、Scene 主配置
- `<world><database>` - 当前 World 挂载的数据库配置，运行时通过 `scene.World.Database` / `scene.World[...]` 访问

## 典型节点关系

```text
Namespace <- process <- scene
machine <- process
Namespace <- WorldGroup <- world <- scene
world <- database
```

未启用 Control Center 时，Namespace / WorldGroup 可以不参与本地运行；允许从 Control Center 回退时，它们必须明确配置，避免跨环境或跨区服分组混用。

这里的 `scene` 最终都会对应成运行时 Root Scene。关于运行时 Scene 的职责、生命周期和组件访问方式，见 `references/ecs/scene.md`。

这里的 `database` 不是单独属于某个 Scene，而是属于 `world`。因此同一个 `world` 下的 Scene 在运行时共享这组数据库配置，并通过 `scene.World` 访问。具体数据库使用方式见 `references/database/index.md`。
