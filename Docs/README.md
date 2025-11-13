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

### 🚧 [03-Advanced](03-Advanced/) - 进阶主题 (规划中)

深入探索 Fantasy Framework 的高级特性和最佳实践。

#### 核心系统
- [ ] 01-ECS.md - Entity-Component-System 详解
- [ ] 02-Scene.md - Scene 和 SubScene 使用
- [ ] 03-Lifecycle.md - Entity 生命周期和 System 系统

#### 网络开发
- [ ] 04-Network.md - 网络协议选择和配置
- [ ] 05-Message.md - Message、Request/Response 使用
- [ ] 06-Protocol.md - .proto 文件编写和代码生成
- [ ] 07-Distributed.md - Server-to-Server 通信
- [ ] 08-SphereEvent.md - SphereEvent 跨服域事件系统

#### 异步编程
- [ ] 09-FTask.md - FTask 和 FCancellationToken
- [ ] 10-CoroutineLock.md - CoroutineLock 使用
- [ ] 11-FlowControl.md - FTaskFlowLock 流量限制

#### 数据持久化
- [ ] 12-Database.md - MongoDB 集成和使用
- [ ] 13-Persistence.md - Entity 数据库操作
- [ ] 14-SeparateTable.md - SeparateTable 层级关系

#### 配置系统
- [ ] 15-ConfigTable.md - Excel 配置表设计和导出
- [ ] 16-CodeGeneration.md - 配置表代码生成工具

#### 高级特性
- [ ] 17-Event.md - Event 系统使用
- [ ] 18-Timer.md - Timer 系统
- [ ] 19-Addressable.md - Addressable 路由
- [ ] 20-HotReload.md - 热重载支持
- [ ] 21-ObjectPool.md - 内存管理和对象池

#### Source Generator
- [ ] 22-SourceGenerator.md - Roslyn Source Generator 原理
- [ ] 23-CustomInterface.md - CustomInterface 注册
- [ ] 24-AOT.md - Native AOT 支持

#### 性能优化
- [ ] 25-Performance.md - 性能优化最佳实践
- [ ] 26-Benchmark.md - Benchmark 使用

#### 部署运维
- [ ] 27-Deployment.md - 服务器部署指南
- [ ] 28-UnityIntegration.md - Unity 客户端集成详解

#### 示例项目
- [ ] 29-ExampleConsole.md - Console 应用示例解析
- [ ] 30-ExampleServer.md - Server 应用示例解析

#### 常见问题
- [ ] FAQ.md - 常见问题解答
- [ ] Troubleshooting.md - 故障排查指南

---

## 📖 推荐学习路径

### 服务器端开发路径 🖥️

如果你是**服务器端开发者**，建议按照以下顺序学习：

1. **快速入门**
   - [01-QuickStart-Server.md](00-GettingStarted/01-QuickStart-Server.md) - 安装和基本配置
   - 🎯 **新手推荐**: 使用 Fantasy CLI 工具 (`fantasy init`) 一键创建项目
   - ⚠️ **macOS/Linux 用户**: CLI 安装后如无法使用，请查看 [配置说明](../Fantasy.Packages/Fantasy.Cil/README.md)

2. **配置和启动**
   - [01-ServerConfiguration.md](01-Server/01-ServerConfiguration.md) - 理解配置文件格式
   - [05-ConfigUsage.md](01-Server/05-ConfigUsage.md) - 在代码中使用配置
   - [02-WritingStartupCode.md](01-Server/02-WritingStartupCode.md) - 编写启动代码
   - [06-LogSystem.md](01-Server/06-LogSystem.md) - 日志系统配置和使用
   - [07-NetworkProtocol.md](01-Server/07-NetworkProtocol.md) - 网络协议目录结构说明
   - [08-NetworkProtocolExporter.md](01-Server/08-NetworkProtocolExporter.md) - 网络协议导出工具使用
   - [03-CommandLineArguments.md](01-Server/03-CommandLineArguments.md) - 配置启动参数

3. **场景初始化**
   - [04-OnCreateScene.md](01-Server/04-OnCreateScene.md) - 场景创建事件处理

4. **进阶主题**（规划中）
   - ECS 系统
   - 网络消息处理
   - 数据库集成
   - 分布式通信

---

### Unity 客户端开发路径 📱

如果你是 **Unity 客户端开发者**，建议按照以下顺序学习：

1. **快速入门**
   - [02-QuickStart-Unity.md](00-GettingStarted/02-QuickStart-Unity.md) - Unity 包安装和配置

2. **客户端启动**
   - [01-WritingStartupCode-Unity.md](02-Unity/01-WritingStartupCode-Unity.md) - Unity 启动代码编写

3. **进阶主题**（规划中）
   - Unity 网络通信
   - Unity ECS 集成
   - HybridCLR 热更新详解

---

### 全栈开发路径 🌐

如果你需要**同时开发服务器端和客户端**，建议：

1. 先学习服务器端快速入门
2. 再学习 Unity 客户端快速入门
3. 深入学习服务器端配置和启动
4. 深入学习客户端启动和网络通信
5. 学习进阶主题（网络协议、分布式、性能优化）

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
- **进阶主题**: `03-Advanced/XX-文档名.md`

---

## 📊 文档编写进度

| 分类 | 已完成 | 规划中 | 总计 | 进度 |
|------|--------|--------|------|------|
| **快速入门** | 2 | 0 | 2 | 100% ✅ |
| **服务器端指南** | 8 | 0 | 8 | 100% ✅ |
| **客户端指南** | 1 | 0 | 1 | 100% ✅ |
| **进阶主题** | 0 | 30 | 30 | 0% 🚧 |
| **总计** | **11** | **30** | **41** | **27%** |

**最后更新**: 2025-11-08

---

## 🔗 相关资源

- **GitHub**: https://github.com/qq362946/Fantasy
- **官方网站**: https://www.code-fantasy.com/
- **问题反馈**: https://github.com/qq362946/Fantasy/issues

---

## 📝 更新日志

### 2025-11-13
- ✅ Fantasy.Cli init命令增加了执行目录参数

### 2025-11-12
- ✅ 修改文档中默认框架的版本号

### 2025-11-08
- ✅ 添加网络协议导出工具使用指南 (08-NetworkProtocolExporter.md)
- ✅ 添加网络协议目录结构说明 (07-NetworkProtocol.md)
- ✅ 添加日志系统使用指南 (06-LogSystem.md)
- ✅ 添加 Fantasy CLI 脚手架工具说明
- ✅ 更新快速入门文档，推荐使用脚手架工具
- ✅ 更新macOS/Linu使用脚手架工具的注意事项

### 2025-11-06
- ✅ 重组文档结构，按功能模块分组
- ✅ 更新文档命名规范
- ✅ 完善学习路径指引
- ✅ 添加进度追踪

### 之前
- ✅ 完成快速入门文档
- ✅ 完成服务器端配置和启动文档
- ✅ 完成 Unity 客户端启动文档
