# HTTP Controller 编写

## 什么时候用 `SceneContextFilter`

如果 Controller 的 Action 里要访问 `Scene`、Entity、Component，或者依赖必须在 Scene 同步上下文里执行，就加：

```csharp
[ServiceFilter(typeof(SceneContextFilter))]
```

`SceneContextFilter` 的作用是把 Controller Action 调度到当前 `Scene` 的线程同步上下文中执行。

## 基本模板

```csharp
using Fantasy.Async;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(SceneContextFilter))]
public class HelloController : ControllerBase
{
    private readonly Scene _scene;

    public HelloController(Scene scene)
    {
        _scene = scene;
    }

    [HttpGet("greet")]
    public async FTask<IActionResult> Greet()
    {
        await FTask.CompletedTask;
        return Ok($"Hello from Fantasy! SceneType:{_scene.SceneType}");
    }
}
```

## 关键点

1. 需要 `Scene` 时，通过构造函数注入
2. 需要在 Scene 线程执行时，加 `[ServiceFilter(typeof(SceneContextFilter))]`
3. 返回 `IActionResult` 风格结果，不要套用网络消息的返回模式
4. `Route("api/[controller]")` 会把类名去掉 `Controller` 后缀后作为路由片段

## 返回值建议

推荐：

- `Ok(...)`
- `NotFound(...)`
- `BadRequest(...)`
- `CreatedAtAction(...)`
- `NoContent()`

不要在 HTTP Controller 里用 `response.ErrorCode` 当成主要返回模式，那是网络消息 Handler 的模式。

## 线程与 Scene 的关系

根据 `SceneContextFilter` 源码：

- HTTP 请求本身来自 ASP.NET Core 请求线程
- 加了 `SceneContextFilter` 后，Action 会切换到 `Scene.ThreadSynchronizationContext` 执行

因此：

- 要访问 Scene 内运行时对象时，优先加这个 Filter
- 如果 Action 只是纯 HTTP 输入输出，不依赖 Scene，可以不加

## 示例：访问 Scene 组件

```csharp
[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(SceneContextFilter))]
public class ProductsController : ControllerBase
{
    private readonly Scene _scene;

    public ProductsController(Scene scene)
    {
        _scene = scene;
    }

    [HttpGet("{id}")]
    public async FTask<IActionResult> GetById(long id)
    {
        var productManage = _scene.GetComponent<ProductManageComponent>();

        if (!productManage.TryGet(id, out var product))
        {
            return NotFound($"Product {id} not found");
        }

        await FTask.CompletedTask;
        return Ok(product);
    }
}
```

## 什么时候不需要 `SceneContextFilter`

以下场景可以不加：

- 纯静态返回
- 纯参数校验
- 不访问 `Scene`、Entity、Component

但如果不确定，优先加上，尤其是在 Fantasy 业务 Controller 中。

## 常见错误

### 错误 1：访问 Scene 运行时对象却没加 `SceneContextFilter`

这会让业务代码运行在线程池请求线程上，容易和 Scene 线程模型冲突。

### 错误 2：把 HTTP Controller 写成消息 Handler 风格

HTTP 用 ASP.NET Core 的返回方式，不要混用 `MessageRPC` 那套模式。

### 错误 3：缓存注入进来的 Scene 引用到别的长生命周期对象里

Controller 是请求期对象；`Scene` 的使用边界应保持清晰，不要随意长期缓存。

## 相关文档

- `http-server.md` - HTTP 服务和中间件配置
- `references/config.md` - HTTP 监听配置属于 Fantasy.config
