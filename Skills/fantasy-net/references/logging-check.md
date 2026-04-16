# 日志审查清单

**本文件用于检查日志接入是否符合 Fantasy 约定。**

## 检查顺序

1. 日志初始化方式是否明确
2. 是否正确注册到 `Entry.Start()`
3. 如果使用 NLog，配置文件是否复制到输出目录
4. Develop / Release 模式切换是否按预期工作
5. 自定义 `ILog` 是否实现完整

## 常见问题

### 错误 1：定义了日志实现但没有传给 `Entry.Start()`

### 错误 2：NLog.config 没复制到输出目录

### 错误 3：改了 ruleName 却没同步 `NLog.cs`

### 错误 4：误以为 Develop 模式会写文件日志

默认不是，Develop 模式只保留控制台输出。

## 审查时重点问自己

1. 当前选的是 ConsoleLog、Fantasy.NLog 还是自定义 ILog
2. 配置文件和代码初始化是否成对出现
3. 模式切换是否真正生效

## 相关文档

- `logging.md`
