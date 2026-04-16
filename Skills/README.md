# fantasy-net

`fantasy-net` 是一个面向 **Fantasy.Net / Fantasy.Unity** 框架的专用开发与代码审查 Skill。

它不是通用 C# 助手，而是围绕 Fantasy 的实际开发方式，为开发者提供 **实现指导、配置说明、规范约束、问题排查、代码审查** 的一体化支持。

这个 Skill 的核心目标是：

- 帮助开发者快速理解和使用 Fantasy 框架
- 按 Fantasy 的规范生成或检查代码
- 降低分布式服务器、ECS、网络通信、配置系统、数据库、HTTP、Unity 客户端接入的使用成本
- 在项目开发过程中统一代码风格、架构边界和最佳实践

## 功能介绍

### 1. 服务端架构与项目接入

- 指导 Fantasy 服务器项目创建与集成
- 检查三层结构（`Entity / Hotfix / Main`）是否合理
- 检查 `Fantasy-Net` 引用、`FANTASY_NET` 宏、`AssemblyHelper.Initialize()`、`Entry.Start()` 等接入要点
- 指导日志系统、配置文件、启动流程的正确搭建

### 2. ECS 与 Scene 体系开发

- 指导 `Entity`、`Component`、`System` 的定义与组织方式
- 解释 `Scene`、`SubScene`、生命周期边界、对象池、层级销毁等机制
- 帮助判断是否需要 `AwakeSystem`、`DestroySystem`、`UpdateSystem`、`TransferOutSystem`、`TransferInSystem`
- 审查 ECS 代码是否符合 Fantasy 的线程模型和生命周期规范

### 3. Event、EventAwaiter、Timer

- 指导本地事件系统 `Event`
- 指导等待-通知机制 `EventAwaiter`
- 指导 `TimerComponent`、`FTask.Wait`、`OnceTimer`、`RepeatedTimer`
- 帮助区分 Event / EventAwaiter / Timer 的使用边界
- 检查是否存在超时遗漏、重复定时器未取消、等待结果未检查、热重载不友好等问题

### 4. 协议、消息与网络 Handler

- 指导 `.proto` 协议定义
- 区分 `Outer` / `Inner`
- 区分 `IMessage`、`IRequest`、`IResponse`、`IAddressRequest`、`IAddressResponse` 等接口
- 指导服务器消息 Handler、Address Handler、Roaming Handler、SphereEvent 处理器编写
- 检查协议和 Handler 是否匹配、是否遗漏导出、命名是否规范

### 5. Address、Roaming、SphereEvent 跨服通信

- 指导服务器间 `Address` 消息通信
- 指导 `Roaming` 漫游系统与 `Terminus` 生命周期
- 指导 `SphereEvent` 跨服域事件系统
- 帮助区分 Address / Roaming / SphereEvent / 本地 Event 的适用场景
- 检查建链、订阅、传送、断线清理、地址缓存、机制边界是否正确

### 6. HTTP 服务器与 Controller

- 指导 Fantasy 中 HTTP 服务的正确配置方式
- 指导 `OnConfigureHttpServices`、`OnConfigureHttpApplication`
- 指导 `SceneContextFilter` 与 `Scene` 注入方式
- 指导 HTTP Controller 编写与中间件配置
- 检查中间件顺序、HTTP 返回语义、是否误用消息 Handler 风格、是否错误访问 Scene 线程模型

### 7. MongoDB 与数据持久化

- 指导 `Fantasy.config` 中 `<world><database>` 的数据库配置
- 指导运行时通过 `scene.World.Database` 获取数据库实例
- 指导 `Save`、`Insert`、`Query`、`Remove`、索引、分页、`isDeserialize` 使用
- 指导 `SeparateTable` 聚合分表存储
- 检查数据库访问是否符合 Fantasy 规范，是否存在并发创建、字段保存顺序、锁缺失、配置悬空等问题

### 8. Fantasy.config 配置系统

- 指导 `machine`、`process`、`world`、`scene`、`database` 等配置项编写
- 帮助理解配置里的 `scene` 和运行时 `Scene` 的对应关系
- 指导多区服、端口、协议、数据库配置
- 检查配置引用关系、端口冲突、无效 world/database/scene 配置等问题

### 9. Unity 客户端接入

- 指导 `Fantasy.Unity` 安装与接入
- 指导编译宏、协议导入、连接方式、Session 使用
- 指导客户端接收服务器主动推送的 Handler 编写
- 检查版本一致性、宏配置、连接方式和 Session 生命周期问题

### 10. 代码审查与规范检查

- 对 Fantasy 项目代码进行针对性的 review
- 检查代码是否符合 Fantasy 框架的推荐写法
- 输出严重问题、规范问题、潜在风险、优化建议
- 不只是指出普通 C# 问题，更会重点检查 Fantasy 特有的：
  - `FTask` 使用
  - Source Generator 自动注册机制
  - Scene 生命周期
  - Event / EventAwaiter / Roaming / Address / SphereEvent 边界
  - 对象池与销毁逻辑
  - 配置与运行时一致性
  - 分布式服务器通信模型

## 推荐安装方式

**推荐优先使用项目安装，而不是全局安装。**

原因：

- 每个项目可以使用自己的 skill 版本
- skill 可以和项目源码、配置、目录结构一起维护
- 不同项目之间不会互相污染
- 更适合团队协作和版本管理
- 更新 skill 时更容易和项目一起提交

## 目录结构

一个标准的 `fantasy-net` skill 目录应包含：

```text
fantasy-net/
├── SKILL.md
├── README.md
├── REFERENCE_GUIDELINES.md
├── references/
├── templates/
```

## 安装方式

下面分别说明 **项目安装** 和 **全局安装**。

---

## Claude 安装

### 项目安装（推荐）

把整个 `fantasy-net` 目录放到项目根目录下的：

```text
<your-project>/.claude/skills/fantasy-net/
```

示例：

```text
/path/to/your-project/.claude/skills/fantasy-net/
```

### 全局安装

如果你的 Claude 环境支持全局 skills，可以放到用户级 Claude skills 目录，例如：

```text
~/.claude/skills/fantasy-net/
```

如果你的 Claude 客户端或环境使用的是其他全局目录，请按对应工具实际目录调整。

---

## Codex 安装

### 项目安装（推荐）

如果你的 Codex 工作流支持项目级 skill / prompt / agent 目录，推荐放到项目根目录下的 Codex 本地配置目录中。

常见项目级目录示例：

```text
<your-project>/.codex/skills/fantasy-net/
```

如果你当前使用的 Codex 版本没有固定的 `skills/` 约定，可以把该目录作为项目内可引用的 skill 包保存，并在你的 Codex 配置里指向它。

### 全局安装

常见用户级目录示例：

```text
~/.codex/skills/fantasy-net/
```

如果你的 Codex 使用别的全局目录，请按实际工具行为调整。

---

## OpenCode 安装

### 项目安装（推荐）

放到项目根目录下：

```text
<your-project>/.opencode/skills/fantasy-net/
```

示例：

```text
/path/to/your-project/.opencode/skills/fantasy-net/
```

### 全局安装

放到用户级目录：

```text
~/.opencode/skills/fantasy-net/
```

---

## OpenClaw 安装

### 项目安装（推荐）

如果你的 OpenClaw 也支持项目级技能目录，推荐使用：

```text
<your-project>/.openclaw/skills/fantasy-net/
```

### 全局安装

用户级目录示例：

```text
~/.openclaw/skills/fantasy-net/
```

如果你的 OpenClaw 版本使用别的目录命名，也可以采用同样思路，把 `fantasy-net` 整个目录放入该工具的全局 skills 目录中。

---

## 安装步骤

### 方式一：复制目录

直接把整个 `fantasy-net` 目录复制到对应位置。

例如 Claude 项目安装：

```text
<your-project>/.claude/skills/fantasy-net/
```

### 方式二：用 Git 子模块或同步脚本维护

如果你要在多个项目中共用同一份 skill，推荐：

- 使用 Git 子模块
- 或者用同步脚本复制到多个项目的 `.claude/skills/`、`.opencode/skills/` 等目录

这样更新起来更可控。

## 安装后如何验证

安装完成后，可以用这些问题测试是否命中：

- `Fantasy 的 EventAwaiter 怎么写？`
- `帮我检查这段 Fantasy 代码有没有问题`
- `Fantasy.config 里 world 下面的 database 应该怎么配？`
- `Roaming 和 SphereEvent 有什么区别？`
- `OnConfigureHttpServices 在 Fantasy HTTP 里怎么用？`
- `帮我看下这个 AddressRPC Handler 是否符合规范`

如果工具能命中 `fantasy-net` 并开始按 Fantasy 的规范回答，就说明安装成功。

## 升级方式

升级 skill 时，建议：

1. 保留整个目录结构不变
2. 覆盖更新 `SKILL.md`、`references/`、`templates/` 等文件
3. 如果是项目安装，和项目一起提交版本变更
4. 如果是全局安装，建议保留更新日志或版本标签，避免多个项目感知不一致

## 推荐实践

- 优先项目安装
- skill 与项目代码一起维护
- skill 升级后，拿真实 Fantasy 项目代码做一次 review 验收
- 大改后同步更新 `README.md` 和 `REFERENCE_GUIDELINES.md`

## 一句话总结

`fantasy-net` 是一个专门服务于 Fantasy 框架开发的智能 Skill，覆盖 **服务端、客户端、ECS、配置、协议、数据库、HTTP、跨服通信、代码审查** 等核心能力，既能指导实现，也能检查问题，帮助团队更高效、更规范地完成 Fantasy 项目开发。
