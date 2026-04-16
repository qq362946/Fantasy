# 服务器项目接入审查清单

**本文件用于检查服务器项目创建或集成 Fantasy 是否完整。**

## 检查顺序

1. 项目目标框架是否满足要求
2. 三层结构或现有项目分层是否合理
3. 是否正确引用 `Fantasy-Net`
4. 是否配置 `FANTASY_NET` 宏
5. 是否补了 `AssemblyHelper.Initialize()` 和 `Entry.Start()`
6. Fantasy.config、日志、程序集加载是否完整

## 常见问题

### 错误 1：目标框架低于 net8.0

### 错误 2：多层项目只改了一层，引用链不完整

### 错误 3：加了 NuGet 引用但没配 `FANTASY_NET`

### 错误 4：遗漏 `AssemblyHelper.Initialize()`

这会导致生成注册无法正确生效。

### 错误 5：`Program.cs` 没正确调用 `Entry.Start()`

### 错误 6：Fantasy.config 或日志系统没有接完整

## 审查时重点问自己

1. 入口项目、Entity、Hotfix、Main 的职责是否清晰
2. 编译宏、NuGet、程序集加载是否完整
3. 配置、日志、启动代码是否都接上了

## 相关文档

- `setup-server.md`
- `config.md`
- `logging.md`
