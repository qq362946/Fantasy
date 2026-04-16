# HTTP 入口

Fantasy 支持在服务器中集成 ASP.NET Core HTTP 服务。HTTP 主题在 Fantasy 里有两类问题，不要混着读：

- HTTP 服务器怎么配置、怎么加认证/授权/CORS/中间件
- HTTP Controller 怎么写、怎么注入 `Scene`、怎么保证在 Scene 线程里执行

## Workflow

```text
配置 HTTP 服务、依赖注入、MVC、认证、授权、中间件 -> http-server.md
编写 Controller、注入 Scene、使用 SceneContextFilter -> http-controller.md
```

## 必记规则

1. Fantasy 的 HTTP 服务配置不是手写 `Program.cs`，而是通过 `OnConfigureHttpServices` 和 `OnConfigureHttpApplication` 事件扩展
2. `HTTPServerNetwork` 会自动注册 `Scene`、`SceneContextFilter`、`AddControllers()` 并在最后 `MapControllers()`
3. 需要在 Scene 线程里执行的 Controller，使用 `[ServiceFilter(typeof(SceneContextFilter))]`
4. Controller 里返回标准 ASP.NET Core `IActionResult`，不要套用网络消息里的 `response.ErrorCode` 模式
5. 中间件顺序很重要，尤其是 CORS、认证、授权

## 子文档

- `http-server.md` - 服务器配置事件和中间件配置
- `http-controller.md` - Controller 写法与 Scene 集成
