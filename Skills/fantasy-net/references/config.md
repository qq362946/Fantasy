# Fantasy.config 入口

Fantasy.config 是框架的 XML 启服配置文件，位于直接引用 Fantasy.Net 包的项目根目录下，与该项目的 `.csproj` 同级。

**完整注释模板见 `templates/Fantasy.config`。** 需要直接落 XML 时再读模板，不要一开始就整份吞进去。

## 配置里的 Scene 和 ECS 里的 Scene 是同一个概念

`Fantasy.config` 里的每一条 `<scene>` 配置，启动后都会在服务器运行时创建出一个对应的 **Root Scene** 实例。

也就是说：

- 配置文件里的 `<scene ... sceneTypeString="Gate" ... />` 不是纯配置项，而是会实际运行一个 `SceneType.Gate` 的 Scene
- 每新增一条 `<scene>`，通常就意味着启动时会多创建一个服务器运行中的 Scene
- 这些运行中的 Root Scene，就是 `references/ecs/scene.md` 里说的 ECS 顶层容器

可以这样理解：

```text
Fantasy.config 里的 <scene> 定义
            ↓
服务器启动时创建对应的 Root Scene
            ↓
该 Scene 再承载自己的 Entity / Component / Timer / Event / Network 等运行时资源
```

所以当用户说“我要加一个 Map Scene”时，实际是在做两件相关的事：

1. 在 `Fantasy.config` 里增加一条 `<scene>` 配置
2. 在代码里为这个 `SceneType` 准备对应的初始化逻辑，例如 `OnCreateScene` 中添加组件

## Workflow

```text
新建项目或从零写配置 -> config-scenarios.md
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

## 核心节点速查

- `<idFactory>` - ID 生成策略，常用 `Default` 或 `World`
- `<network>` - 内网协议与消息大小
- `<session>` - 空闲检测超时配置
- `<server>` - 机器、进程、World、数据库、Scene 主配置
- `<world><database>` - 当前 World 挂载的数据库配置，运行时通过 `scene.World.Database` / `scene.World[...]` 访问

## 典型节点关系

```text
machine <- process <- scene
world <- scene
world <- database
```

其中这里的 `scene`，最终都会对应成运行时里的 Root Scene。关于运行时 Scene 的职责、生命周期和组件访问方式，见 `references/ecs/scene.md`。

这里的 `database` 不是单独属于某个 Scene，而是属于 `world`。因此同一个 `world` 下的 Scene 在运行时共享这组数据库配置，并通过 `scene.World` 访问。具体数据库使用方式见 `references/database/index.md`。
