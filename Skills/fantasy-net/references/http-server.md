# HTTP 服务器配置

Fantasy 的 HTTP 服务由 `HTTPServerNetwork` 启动，底层使用 ASP.NET Core。框架已经帮你完成基础启动流程，你主要通过两个事件扩展：

- `OnConfigureHttpServices` - 服务配置阶段
- `OnConfigureHttpApplication` - 应用 / 中间件配置阶段

## 框架启动流程

根据 `HTTPServerNetwork` 源码，HTTP 启动顺序大致是：

```text
1. 创建 WebApplicationBuilder
2. 注册 Scene 到 DI 容器
3. 注册 SceneContextFilter
4. AddControllers() 并建立 MVC Builder
5. 发布 OnConfigureHttpServices
6. 收集程序集中的 Controllers
7. builder.Build()
8. 开发环境启用 DeveloperExceptionPage
9. 发布 OnConfigureHttpApplication
10. app.MapControllers()
11. RunAsync()
```

这意味着：

- 服务注册放 `OnConfigureHttpServices`
- 中间件顺序放 `OnConfigureHttpApplication`
- Controller 路由映射框架最后自动做，不需要你自己再 `MapControllers()`

## OnConfigureHttpServices

用于配置：

- 依赖注入服务
- MVC 选项
- JSON 序列化
- 全局过滤器
- 认证和授权服务
- CORS 策略

事件参数：

- `Scene` - 当前 HTTP 服务器所属 Scene
- `Builder` - `WebApplicationBuilder`
- `MvcBuilder` - `IMvcBuilder`

### 示例

```csharp
public sealed class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        self.MvcBuilder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });

        self.Builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        self.Builder.Services.AddSingleton<IPlayerService, PlayerService>();

        await FTask.CompletedTask;
    }
}
```

## OnConfigureHttpApplication

用于配置：

- CORS 中间件
- 认证 / 授权中间件
- 请求日志中间件
- 自定义响应头
- 自定义异常处理
- 限流等横切逻辑

事件参数：

- `Scene` - 当前 HTTP 服务器所属 Scene
- `Application` - `WebApplication`

### 示例

```csharp
public sealed class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        var app = self.Application;

        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();

        app.Use(async (context, next) =>
        {
            var start = DateTime.UtcNow;
            await next.Invoke();
            var duration = (DateTime.UtcNow - start).TotalMilliseconds;
            Log.Info($"[HTTP] {context.Request.Method} {context.Request.Path} - {duration}ms");
        });

        await FTask.CompletedTask;
    }
}
```

## 中间件顺序建议

推荐顺序：

1. 异常处理
2. CORS
3. 认证
4. 授权
5. 自定义中间件（日志、限流、响应头等）

特别注意：

- CORS 通常要在认证之前
- `MapControllers()` 由框架最后自动调用，不要重复映射

## 适合放在这里的配置

- JWT 认证
- 授权策略
- CORS
- 全局过滤器
- 自定义中间件
- 请求日志
- 统一异常响应

## 不要放在这里的内容

- 具体业务 Action 实现
- Controller 路由本身
- 业务层数据查询细节

这些放到 `http-controller.md` 里对应的 Controller 或服务中。
