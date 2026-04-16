# HTTP 审查清单

**本文件用于检查 HTTP 服务和 Controller 代码。**

## 检查顺序

1. HTTP 服务配置是否放在正确事件里
2. 中间件顺序是否正确
3. Controller 是否正确使用 `SceneContextFilter`
4. 返回模式是否符合 HTTP 语义
5. 是否重复或错误映射 Controllers

## 常见问题

### 错误 1：把服务注册、中间件和业务 Action 混在一起

- 服务配置 -> `OnConfigureHttpServices`
- 中间件顺序 -> `OnConfigureHttpApplication`
- 具体 Action -> Controller

### 错误 2：重复 `MapControllers()`

`HTTPServerNetwork` 最后会自动调用 `app.MapControllers()`，不要重复映射。

### 错误 3：中间件顺序错误

重点检查：

- CORS 是否放在认证前
- 认证是否在授权前
- 自定义中间件是否放在合适位置

### 错误 4：访问 Scene 运行时对象却没加 `SceneContextFilter`

如果 Controller 访问 `Scene`、Entity、Component，应优先加：

```csharp
[ServiceFilter(typeof(SceneContextFilter))]
```

### 错误 5：把 HTTP 返回写成消息 Handler 风格

HTTP 应返回标准 `IActionResult` 语义，不要套用 `response.ErrorCode` 模式。

## 审查时重点问自己

1. 这段逻辑属于服务配置、中间件还是 Controller 业务
2. 是否误重复映射路由
3. 是否需要 `SceneContextFilter`
4. 返回值是否符合 HTTP 语义

## 相关文档

- `http.md`
- `http-server.md`
- `http-controller.md`
