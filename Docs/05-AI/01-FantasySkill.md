# Fantasy AI Skill 与 Vibe Coding 指南

Fantasy 现在不仅是一个高性能、分布式、零反射的 C# 游戏服务器框架，也已经具备了面向 AI 的开发支持能力。

通过专用的 `fantasy-net` Skill，你可以把 Claude、Codex、OpenCode、OpenClaw 等 AI Agent 直接变成 Fantasy 框架开发助手，用来学习框架、生成代码、检查规范、排查问题，显著降低上手门槛。

---

## 🤖 什么是 fantasy-net Skill

`fantasy-net` 是一个专门为 Fantasy / Fantasy.Net / Fantasy.Unity 设计的 Skill。

它不是通用的 C# Prompt，而是围绕 Fantasy 的实际开发方式构建的知识层和规则层，已经覆盖了：

- 服务端项目接入与启动
- ECS、Scene、SubScene、生命周期系统
- Event、EventAwaiter、Timer
- 协议、Handler、Address、Roaming、SphereEvent
- Fantasy.config、MongoDB、SeparateTable
- HTTP 服务、Controller、SceneContextFilter
- Unity 客户端连接、Session、推送处理
- Fantasy 项目代码审查与规范检查

也就是说，你现在可以直接借助 AI Agent：

- 学习 Fantasy 框架
- 生成符合 Fantasy 风格的代码
- 检查现有代码是否符合规范
- 排查配置、跨服通信、数据库和运行时问题

---

## ✨ AI 赋能能带来什么

对 Fantasy 来说，AI 赋能的价值不只是“能问问题”，而是：

- **减少学习成本**：框架概念多，但现在可以边问边学
- **减少试错成本**：机制边界清晰，AI 能更快指出你是否用错 Event、Roaming、Address、SphereEvent 等
- **减少样板成本**：Entity / Component / System、协议、Handler、配置等都能更快生成
- **减少排查成本**：当代码能跑但不符合 Fantasy 规范时，AI 也能指出隐患

这意味着 Fantasy 不只是“可以配合 AI 用”，而是已经在知识组织层面适配了 AI 开发方式。

---

## 🎨 为什么 Fantasy 特别适合 Vibe Coding

Fantasy 天然适合 Vibe Coding，原因不是“它支持 AI”这么简单，而是因为它本身具有非常清晰的工程边界：

- Entity / Component / System 职责明确
- Scene / SubScene 生命周期边界清晰
- 协议、Handler、跨服通信模型边界稳定
- Source Generator 自动注册，减少手工接线和重复样板代码
- 配置、数据库、HTTP、Unity 接入路径统一

在此基础上，`fantasy-net` Skill 又把这些规则、最佳实践和常见错误结构化，交给 AI Agent 理解和调用。

因此 Fantasy 非常适合：

- 边问边学
- 边聊边做
- 边写边审
- 边改边验证

无论你是个人开发者、小团队、原型验证项目，还是正在构建正式服务端，都会明显感受到开发体验提升。

---

## 🚀 AI 可以帮你做什么

借助 `fantasy-net` Skill，你可以直接让 AI 帮你：

- 创建 Entity / Component / System
- 编写 Event、EventAwaiter、Timer 逻辑
- 定义协议并实现消息 Handler
- 设计 Address、Roaming、SphereEvent 跨服通信
- 编写 Fantasy.config
- 接入 MongoDB 和 SeparateTable
- 配置 HTTP 服务与 Controller
- 接入 Unity 客户端
- 对 Fantasy 项目代码做 review、规范检查与问题排查

---

## 🧠 示例：你可以这样问 AI

### 学习框架

- `Fantasy 里的 EventAwaiter 和 Event 有什么区别？`
- `Scene、SubScene、Map 分线在 Fantasy 里应该怎么理解？`
- `Roaming、Address、SphereEvent 分别适合什么场景？`

### 生成代码

- `帮我在 Fantasy 里创建一个 Player 实体和对应的 AwakeSystem`
- `帮我写一个 EventAwaiter 版本的玩家确认流程`
- `帮我定义一组 Outer 协议和对应的 MessageRPC Handler`
- `帮我写一个包含 Gate、Map、MapControl、MongoDB 的 Fantasy.config`

### 排查问题

- `帮我检查这段 Roaming 代码有没有问题`
- `这段 AddressRPC Handler 是否符合 Fantasy 规范？`
- `为什么我的 Scene.World.Database 查询出来的数据用起来不对？`
- `帮我看看这个 SphereEvent 退订逻辑有没有问题`

### 代码审查

- `请按 Fantasy 规范 review 这段代码`
- `帮我检查这段代码有没有违反 Fantasy 的线程模型和生命周期约束`
- `看看这段 HTTP Controller 是否需要 SceneContextFilter`

---

## 📦 如何开始使用

如果你想立即使用这个能力，可以查看：

- **AI Skill 安装说明与功能介绍**：[`../../Skills/README.md`](../../Skills/README.md)

推荐优先采用**项目安装**方式，把 skill 跟项目代码一起维护，这样最稳定、最适合团队协作。

---

## ✅ 一句话总结

Fantasy 不只是一个强大的分布式游戏服务器框架，也已经具备了成熟的 AI 赋能开发体验。通过 `fantasy-net` Skill，你可以更轻松地学习框架、生成代码、审查问题和进行 Vibe Coding，把更多精力放在真正的游戏业务设计上。
