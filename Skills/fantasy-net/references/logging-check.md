# 日志审查清单

**本文件用于检查日志接入是否符合 Fantasy 约定。**

## 检查顺序

1. 日志初始化方式是否明确
2. 是否正确注册到 `Entry.Start()`
3. 如果使用 NLog，配置文件是否复制到输出目录
4. Develop / Release 模式切换是否按预期工作
5. 自定义 `ILog` 是否实现完整
6. Scene 日志是否按 `SceneType_SceneId` 分文件

## 常见问题

### 错误 1：定义了日志实现但没有传给 `Entry.Start()`

### 错误 2：NLog.config 没复制到输出目录

### 错误 3：改了 `Console*` ruleName 却没同步 `NLog.cs`

Release 模式按名称移除控制台规则，名称不一致会导致服务器继续输出控制台。

### 错误 4：仍按旧行为认为 Develop 只输出控制台

当前 Develop 同时输出控制台和文件，并使用立即刷新；Release 只输出文件并使用缓冲刷新。

### 错误 5：自定义 `ILog` 仍实现旧接口

检查：

- `Initialize(string appId, ProcessMode processMode)`
- 五个日志级别的普通和格式化重载
- 五个日志级别带 `sceneName` 的普通和格式化重载

### 错误 6：所有 Scene 日志都写进默认 `Log` 文件

需要按 Scene 分文件时调用 `Log.Debug/Info/Warning/Error(scene, ...)`，不要丢失 Scene 参数。

## 审查时重点问自己

1. 当前选的是 ConsoleLog、Fantasy.NLog 还是自定义 ILog
2. 配置文件和代码初始化是否成对出现
3. 模式切换是否真正生效
4. appId 和 sceneName 是否正确进入文件路径

## 相关文档

- `logging.md`
