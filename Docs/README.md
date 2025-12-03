# Fantasy Framework 使用指南

欢迎使用 Fantasy Framework！本指南将帮助你快速上手并深入了解框架的各个功能模块。

---

## 📚 文档结构

文档按照**学习路径**和**功能模块**组织，分为以下几个部分：

### 🚀 [00-GettingStarted](00-GettingStarted/) - 快速入门

新手必读，快速了解 Fantasy Framework 的基本使用。

- ✅ [01-QuickStart-Server.md](00-GettingStarted/01-QuickStart-Server.md) - 服务器端快速开始
  - 🎯 **推荐**: 使用 Fantasy CLI 脚手架工具快速创建项目
  - 安装 Fantasy Framework (NuGet 或源码)
  - 创建配置文件
  - 推荐的项目结构
  - ⚠️ **macOS/Linux 用户**: 如果安装 CLI 后无法使用命令，请查看 [Fantasy CLI 文档](../Fantasy.Packages/Fantasy.Cil/README.md) 配置 PATH

- ✅ [02-QuickStart-Unity.md](00-GettingStarted/02-QuickStart-Unity.md) - Unity 客户端快速开始
  - 安装 Fantasy.Unity 包
  - 配置 Unity 环境
  - 连接服务器示例

---

### 📱 [01-Unity](02-Unity/) - Unity 客户端开发指南

Unity 客户端开发的完整指南。

- ✅ [01-WritingStartupCode-Unity.md](02-Unity/01-WritingStartupCode-Unity.md) - Unity 启动代码编写
    - Unity 与 .NET 的差异
    - 基础 Unity 启动流程
    - HybridCLR 热更新环境配置
    - 常见问题解答
- ✅ [02-FantasyRuntime.md](02-Unity/02-FantasyRuntime.md) - FantasyRuntime 组件使用指南
    - FantasyRuntime 组件模式详解
    - Runtime 静态类模式详解
    - 多实例管理
    - 最佳实践

---

### 🖥️ [02-Server](01-Server/) - 服务器端开发指南

服务器端完整的开发指南，从配置到启动，从基础到进阶。

- ✅ [01-ServerConfiguration.md](01-Server/01-ServerConfiguration.md) - Fantasy.config 配置文件详解
  - 网络配置 (network)
  - 会话配置 (session)
  - 机器、进程、世界、场景配置
  - 配置最佳实践

- ✅ [02-WritingStartupCode.md](01-Server/02-WritingStartupCode.md) - 编写启动代码
  - AssemblyHelper 的作用和实现
  - ModuleInitializer 与 Source Generator
  - 服务器启动代码编写
  - 常见问题解答

- ✅ [03-CommandLineArguments.md](01-Server/03-CommandLineArguments.md) - 命令行参数配置
  - 命令行参数说明 (RuntimeMode, ProcessId, ProcessType, StartupGroup)
  - 开发环境配置 (launchSettings.json)
  - 生产环境配置 (Shell, systemd, Docker)
  - 常用启动场景

- ✅ [04-OnCreateScene.md](01-Server/04-OnCreateScene.md) - OnCreateScene 事件使用指南
  - OnCreateScene 事件触发时机
  - 创建事件处理器
  - 常见使用场景（组件挂载、配置加载、定时任务）
  - 最佳实践

- ✅ [05-ConfigUsage.md](01-Server/05-ConfigUsage.md) - 配置系统使用指南
    - 机器配置 (MachineConfig)
    - 进程配置 (ProcessConfig)
    - 世界配置 (WorldConfig)
    - 场景配置 (SceneConfig)
    - Source Generator 自动生成的代码 (SceneType、DatabaseName)

- ✅ [06-LogSystem.md](01-Server/06-LogSystem.md) - 日志系统使用指南
    - 使用内置 NLog 扩展
    - 实现自定义日志系统
    - 将日志系统注册到框架
    - 日志 API 使用和最佳实践

- ✅ [07-NetworkProtocol.md](01-Server/07-NetworkProtocol.md) - 网络协议目录结构说明
    - NetworkProtocol 目录获取方式
    - Outer 和 Inner 协议文件夹
    - 协议接口类型 (IMessage, IRequest/IResponse, IRouteMessage 等)
    - 协议定义规范和支持的数据类型
    - RouteType 和 RoamingType 配置

- ✅ [08-NetworkProtocolExporter.md](01-Server/08-NetworkProtocolExporter.md) - 网络协议导出工具使用指南
    - 导出工具获取方式 (Fantasy CLI、已编译工具、源码编译)
    - ExporterSettings.json 配置文件详解
    - 交互式运行和命令行参数运行
    - 生成的代码结构 (协议类、OpCode、Helper 扩展方法)
    - 自定义代码模板和错误检测
    - 团队协作和 CI/CD 集成最佳实践

---

### 🌐 [03-Networking](03-Networking/) - 网络通信基础

掌握客户端与服务器之间的通信机制，是 Fantasy 开发的核心技能。

- ✅ [01-Session.md](03-Networking/01-Session.md) - Session 使用指南
  - Session 的获取方式（客户端和服务器端）
  - 发送单向消息 - Send()
  - 发送 RPC 请求 - Call()
  - Session 生命周期管理
  - 心跳和超时配置
  - 完整的通信示例和最佳实践

- ✅ [02-MessageHandler.md](03-Networking/02-MessageHandler.md) - 消息处理器实现指南
  - Message&lt;T&gt; - 处理单向消息
  - MessageRPC&lt;TRequest, TResponse&gt; - 处理 RPC 请求
  - Handler 自动注册机制（Source Generator）
  - Reply() 方法的使用
  - 错误处理和异常管理
  - 完整的业务场景示例

---

### 🚧 [04-Advanced](04-Advanced/) - 进阶主题

深入探索 Fantasy Framework 的高级特性和最佳实践。

#### 核心系统
- ✅ [01-ECS.md](04-Advanced/CoreSystems/01-ECS.md) - Entity-Component-System 详解
- ✅ [02-ISupportedMultiEntity.md](04-Advanced/CoreSystems/02-ISupportedMultiEntity.md) - 多实例组件详解
- ✅ [03-Scene.md](04-Advanced/CoreSystems/03-Scene.md) - Scene 和 SubScene 使用
- ✅ [04-Event.md](04-Advanced/CoreSystems/04-Event.md) - Event 系统使用指南

#### 数据持久化
- ✅ [05-Database.md](04-Advanced/Database/14-Database.md) - MongoDB 集成和使用
- ✅ [06-SeparateTable.md](04-Advanced/Database/16-SeparateTable.md) - SeparateTable 分表存储详解

#### 网络开发
- ✅ [07-Address消息.md](04-Advanced/NetworkDevelopment/06-Address消息.md) - Address 消息 - 服务器间实体通信
- ✅ [08-Roaming.md](04-Advanced/NetworkDevelopment/08-Roaming.md) - Roaming 漫游消息 - 分布式实体路由
- ✅ [09-SphereEvent.md](04-Advanced/NetworkDevelopment/09-SphereEvent.md) - SphereEvent 跨服域事件系统
  - 分布式事件发布-订阅机制
  - 定义 SphereEvent 事件类和处理器
  - 订阅远程事件和发布事件
  - 跨服务器的业务事件通知
  - 完整使用示例（公会战报通知）

#### 高级特性
- [ ] 11-Timer.md - Timer 系统

#### 部署运维
- [ ] 14-Deployment.md - 服务器部署指南

#### 示例项目
- [ ] 31-ExampleConsole.md - Console 应用示例解析
- [ ] 32-ExampleServer.md - Server 应用示例解析

#### 常见问题
- [ ] FAQ.md - 常见问题解答
- [ ] Troubleshooting.md - 故障排查指南

---

### 全栈开发路径 🌐

如果你需要**同时开发服务器端和客户端**，建议：

1. 先学习服务器端快速入门
2. 再学习 Unity 客户端快速入门
3. 深入学习服务器端配置和启动
4. 学习网络协议定义和导出
5. **学习网络通信基础（Session 和 MessageHandler）**
6. 深入学习客户端启动和网络通信
7. 学习进阶主题（ECS、分布式、数据库、性能优化）

---

## 🛠️ 贡献指南

如果你想为文档做出贡献，请遵循以下规范：

- ✅ 使用清晰的标题和章节划分
- ✅ 提供完整的代码示例
- ✅ 包含实际的使用场景
- ✅ 添加必要的注意事项和最佳实践
- ✅ 文档命名遵循现有规范

### 文档命名规范

- **快速入门**: `00-GettingStarted/XX-文档名.md`
- **服务器端**: `01-Server/XX-文档名.md`
- **客户端**: `02-Unity/XX-文档名.md`
- **网络通信**: `03-Networking/XX-文档名.md`
- **进阶主题**: `04-Advanced/XX-文档名.md`

---

## 🔗 相关资源

- **GitHub**: https://github.com/qq362946/Fantasy
- **官方网站**: https://www.code-fantasy.com/
- **问题反馈**: https://github.com/qq362946/Fantasy/issues
