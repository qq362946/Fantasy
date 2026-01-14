# HTTP 服务器配置事件使用指南

本指南将介绍如何使用 `OnConfigureHttpServices` 和 `OnConfigureHttpApplication` 事件来配置 HTTP 服务器，包括依赖注入、中间件、认证授权等。

## 前置步骤

在开始使用 HTTP 服务器配置事件之前，请确保已完成以下步骤：

1. ✅ 已完成服务器启动代码的编写
2. ✅ 已配置好 `Fantasy.config` 文件，并配置了 HTTP 网络监听
3. ✅ 了解 ASP.NET Core 的基本概念（可选但推荐）

如果你还没有完成这些步骤，请先阅读：
- [编写启动代码](02-WritingStartupCode.md)
- [Fantasy.config 配置文件详解](01-ServerConfiguration.md)

---

## 什么是 HTTP 服务器配置事件？

Fantasy Framework 提供了两个配置事件，让你可以完全控制 HTTP 服务器的行为：

1. **`OnConfigureHttpServices`** - 服务配置阶段（对应 ASP.NET Core 的 `ConfigureServices`）
2. **`OnConfigureHttpApplication`** - 应用配置阶段（对应 ASP.NET Core 的 `Configure`）

这两个事件在 HTTP 服务器启动时自动触发，允许你：
- 注册依赖注入服务
- 配置 MVC 选项（JSON 序列化、过滤器、验证等）
- 添加自定义中间件
- 配置认证和授权
- 添加 CORS 策略

### 触发时机

```
HTTP 服务器启动流程:
┌─────────────────────────────────────────────────────┐
│ 1. HTTPServerNetwork.Initialize()                  │
│    ├─ 创建 WebApplicationBuilder                    │
│    ├─ 注册基础服务 (Scene, SceneContextFilter)      │
│    └─ 创建 MvcBuilder (框架默认 JSON 配置)           │
├─────────────────────────────────────────────────────┤
│ 2. 发布 OnConfigureHttpServices 事件 ⬅️ 服务配置    │
│    └─ 你可以配置:                                    │
│        ├─ MVC 选项 (JSON、过滤器、验证)              │
│        └─ 依赖注入服务 (认证、授权、自定义服务)       │
├─────────────────────────────────────────────────────┤
│ 3. 添加程序集控制器 & builder.Build()               │
├─────────────────────────────────────────────────────┤
│ 4. 添加默认异常处理 (开发环境)                       │
├─────────────────────────────────────────────────────┤
│ 5. 发布 OnConfigureHttpApplication 事件 ⬅️ 中间件配置│
│    └─ 你可以添加:                                    │
│        ├─ CORS 中间件                                │
│        ├─ 认证/授权中间件                            │
│        └─ 自定义中间件                               │
├─────────────────────────────────────────────────────┤
│ 6. 注册路由 (MapControllers) & 启动监听             │
└─────────────────────────────────────────────────────┘
```

**重要特性：**
- ✅ 符合 ASP.NET Core 的标准配置模式
- ✅ 提供框架默认配置（开箱即用）
- ✅ 支持完全自定义（用户可覆盖所有默认行为）
- ✅ 清晰的执行顺序（服务配置 → 中间件配置 → 路由注册）

---

## 事件一：OnConfigureHttpServices - 服务配置

### 事件参数

`OnConfigureHttpServices` 定义在 `/Fantasy.Net/Runtime/Core/Network/Protocol/HTTP/HTTPServerNetwork.cs`:

```csharp
/// <summary>
/// HTTP服务配置事件 - 用于配置依赖注入服务
/// 对应 ASP.NET Core 的 ConfigureServices 阶段
/// </summary>
public struct OnConfigureHttpServices
{
    /// <summary>
    /// 当前所属的Scene
    /// </summary>
    public readonly Scene Scene;

    /// <summary>
    /// WebApplicationBuilder，用于配置服务容器
    /// </summary>
    public readonly WebApplicationBuilder Builder;

    /// <summary>
    /// IMvcBuilder，用于配置MVC选项、过滤器、JSON序列化等
    /// </summary>
    public readonly IMvcBuilder MvcBuilder;
}
```

**可用属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Scene` | `Scene` | 当前 HTTP 服务器所属的场景实例 |
| `Builder` | `WebApplicationBuilder` | ASP.NET Core 应用构建器，用于注册服务 |
| `MvcBuilder` | `IMvcBuilder` | MVC 构建器，用于配置 MVC 选项 |

### 创建事件处理器

创建一个继承 `AsyncEventSystem<OnConfigureHttpServices>` 的类：

```csharp
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json;

namespace Server.Hotfix
{
    /// <summary>
    /// HTTP 服务配置处理器
    /// </summary>
    public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
    {
        protected override async FTask Handler(OnConfigureHttpServices self)
        {
            // 1. 配置 JSON 序列化选项
            self.MvcBuilder.AddJsonOptions(options =>
            {
                // 使用驼峰命名
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                // 启用格式化输出（开发环境）
                options.JsonSerializerOptions.WriteIndented = true;
                // 忽略空值
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

            // 2. 添加全局过滤器
            self.MvcBuilder.AddMvcOptions(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
                options.Filters.Add<ValidationFilter>();
            });

            // 3. 注册认证服务
            self.Builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "MyGameServer",
                        ValidAudience = "MyGameClient",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("YourSuperSecretKeyHere"))
                    };
                });

            // 4. 注册授权策略
            self.Builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("PlayerOnly", policy =>
                    policy.RequireRole("Player"));
            });

            // 5. 注册自定义服务
            self.Builder.Services.AddSingleton<IPlayerService, PlayerService>();
            self.Builder.Services.AddScoped<IGameRepository, GameRepository>();

            // 6. 配置 CORS（稍后在中间件中启用）
            self.Builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            Log.Info($"HTTP 服务配置完成: Scene {self.Scene.SceneConfigId}");

            await FTask.CompletedTask;
        }
    }
}
```

### 常见配置场景

#### 1. 自定义 JSON 序列化

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        self.MvcBuilder.AddJsonOptions(options =>
        {
            // 驼峰命名
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            // 枚举转字符串
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter());

            // 忽略循环引用
            options.JsonSerializerOptions.ReferenceHandler =
                System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

            // 格式化输出
            options.JsonSerializerOptions.WriteIndented = true;
        });

        await FTask.CompletedTask;
    }
}
```

#### 2. 添加全局过滤器

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        self.MvcBuilder.AddMvcOptions(options =>
        {
            // 异常过滤器
            options.Filters.Add<GlobalExceptionFilter>();

            // 验证过滤器
            options.Filters.Add<ModelValidationFilter>();

            // 授权过滤器
            options.Filters.Add(new AuthorizeFilter("DefaultPolicy"));
        });

        await FTask.CompletedTask;
    }
}
```

#### 3. 配置 JWT 认证

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        var jwtSettings = LoadJwtSettings(); // 从配置加载

        self.Builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };

                // 从消息头或查询字符串获取 Token
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        await FTask.CompletedTask;
    }
}
```

#### 4. 注册依赖注入服务

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        // Singleton - 整个应用生命周期单例
        self.Builder.Services.AddSingleton<IGameConfigService, GameConfigService>();

        // Scoped - 每个请求一个实例
        self.Builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();

        // Transient - 每次注入创建新实例
        self.Builder.Services.AddTransient<IEmailService, EmailService>();

        // 使用工厂方法注册
        self.Builder.Services.AddSingleton<IGameWorld>(sp =>
        {
            var scene = sp.GetRequiredService<Scene>();
            return new GameWorld(scene);
        });

        await FTask.CompletedTask;
    }
}
```

---

## 事件二：OnConfigureHttpApplication - 中间件配置

### 事件参数

`OnConfigureHttpApplication` 定义在 `/Fantasy.Net/Runtime/Core/Network/Protocol/HTTP/HTTPServerNetwork.cs`:

```csharp
/// <summary>
/// HTTP应用配置事件 - 用于配置中间件管道
/// 对应 ASP.NET Core 的 Configure 阶段
/// </summary>
public struct OnConfigureHttpApplication
{
    /// <summary>
    /// 当前所属的Scene
    /// </summary>
    public readonly Scene Scene;

    /// <summary>
    /// WebApplication，用于配置请求管道和中间件
    /// </summary>
    public readonly WebApplication Application;
}
```

**可用属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Scene` | `Scene` | 当前 HTTP 服务器所属的场景实例 |
| `Application` | `WebApplication` | ASP.NET Core 应用实例，用于配置中间件管道 |

### 创建事件处理器

```csharp
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Builder;

namespace Server.Hotfix
{
    /// <summary>
    /// HTTP 应用配置处理器
    /// </summary>
    public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
    {
        protected override async FTask Handler(OnConfigureHttpApplication self)
        {
            var app = self.Application;

            // 1. 启用 CORS（必须在认证之前）
            app.UseCors("AllowAll");

            // 2. 启用认证
            app.UseAuthentication();

            // 3. 启用授权
            app.UseAuthorization();

            // 4. 自定义请求日志中间件
            app.Use(async (context, next) =>
            {
                var start = DateTime.UtcNow;
                var path = context.Request.Path;
                var method = context.Request.Method;

                Log.Info($"[HTTP] {method} {path} - 请求开始");

                try
                {
                    await next.Invoke();

                    var duration = DateTime.UtcNow - start;
                    var statusCode = context.Response.StatusCode;
                    Log.Info($"[HTTP] {method} {path} - {statusCode} ({duration.TotalMilliseconds}ms)");
                }
                catch (Exception e)
                {
                    Log.Error($"[HTTP] {method} {path} - 异常: {e.Message}");
                    throw;
                }
            });

            // 5. 自定义响应头中间件
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Server-Version", "1.0.0");
                context.Response.Headers.Add("X-Powered-By", "Fantasy Framework");
                await next.Invoke();
            });

            Log.Info($"HTTP 应用配置完成: Scene {self.Scene.SceneConfigId}");

            await FTask.CompletedTask;
        }
    }
}
```

### 中间件执行顺序

中间件按照添加的顺序执行，遵循"洋葱模型"：

```
请求 → 中间件1 → 中间件2 → 中间件3 → 控制器
       ↓          ↓          ↓          ↓
响应 ← 中间件1 ← 中间件2 ← 中间件3 ← 控制器
```

**推荐的中间件顺序：**

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        var app = self.Application;

        // 1. 异常处理（框架已自动添加，如需自定义可覆盖）
        // app.UseExceptionHandler("/Error");

        // 2. HTTPS 重定向（生产环境）
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // 3. 静态文件（如果需要）
        // app.UseStaticFiles();

        // 4. 路由（框架已自动添加）
        // app.UseRouting();

        // 5. CORS
        app.UseCors("AllowAll");

        // 6. 认证
        app.UseAuthentication();

        // 7. 授权
        app.UseAuthorization();

        // 8. 自定义中间件
        app.Use(async (context, next) => { /* ... */ });

        // 9. 端点映射（框架已自动添加 MapControllers）

        await FTask.CompletedTask;
    }
}
```

### 常见中间件场景

#### 1. CORS 配置

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        // 方式1：使用预定义策略（需要在 OnConfigureHttpServices 中配置）
        self.Application.UseCors("AllowAll");

        // 方式2：直接配置
        self.Application.UseCors(builder =>
        {
            builder.WithOrigins("https://game.example.com")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });

        await FTask.CompletedTask;
    }
}
```

#### 2. 请求日志中间件

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.Use(async (context, next) =>
        {
            var requestId = Guid.NewGuid().ToString("N");
            context.Items["RequestId"] = requestId;

            var start = DateTime.UtcNow;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var ip = context.Connection.RemoteIpAddress?.ToString();

            Log.Info($"[{requestId}] {method} {path} - IP: {ip}");

            try
            {
                await next.Invoke();

                var duration = (DateTime.UtcNow - start).TotalMilliseconds;
                var status = context.Response.StatusCode;
                Log.Info($"[{requestId}] {status} - {duration}ms");
            }
            catch (Exception e)
            {
                Log.Error($"[{requestId}] 异常: {e}");
                throw;
            }
        });

        await FTask.CompletedTask;
    }
}
```

#### 3. 限流中间件

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    private static readonly Dictionary<string, (int Count, DateTime Window)> _rateLimits = new();
    private static readonly object _lock = new object();

    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.Use(async (context, next) =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                if (_rateLimits.TryGetValue(ip, out var limit))
                {
                    if (now - limit.Window < TimeSpan.FromMinutes(1))
                    {
                        if (limit.Count >= 100) // 每分钟最多100次请求
                        {
                            context.Response.StatusCode = 429; // Too Many Requests
                            await context.Response.WriteAsync("Rate limit exceeded");
                            return;
                        }
                        _rateLimits[ip] = (limit.Count + 1, limit.Window);
                    }
                    else
                    {
                        _rateLimits[ip] = (1, now);
                    }
                }
                else
                {
                    _rateLimits[ip] = (1, now);
                }
            }

            await next.Invoke();
        });

        await FTask.CompletedTask;
    }
}
```

#### 4. 自定义异常处理

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var result = new
                {
                    error = "Internal Server Error",
                    message = exception?.Message ?? "Unknown error",
                    requestId = context.Items["RequestId"]?.ToString()
                };

                Log.Error($"未处理异常: {exception}");

                await context.Response.WriteAsJsonAsync(result);
            });
        });

        await FTask.CompletedTask;
    }
}
```

---

## 完整示例：游戏 HTTP API 服务器

这是一个完整的游戏 HTTP API 服务器配置示例，包括认证、授权、CORS、日志等：

### 1. 服务配置（OnConfigureHttpServices）

```csharp
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Hotfix
{
    public class GameHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
    {
        protected override async FTask Handler(OnConfigureHttpServices self)
        {
            // 1. 配置 JSON 序列化
            self.MvcBuilder.AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // 2. 添加全局过滤器
            self.MvcBuilder.AddMvcOptions(options =>
            {
                options.Filters.Add<GameExceptionFilter>();
                options.Filters.Add<ModelValidationFilter>();
            });

            // 3. 配置 JWT 认证
            var jwtSecret = "YourSuperSecretKeyForJwtTokenGeneration123!";
            self.Builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "GameServer",
                        ValidAudience = "GameClient",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSecret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // 4. 配置授权策略
            self.Builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Player", policy =>
                    policy.RequireRole("Player", "Admin"));
                options.AddPolicy("Admin", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("VIP", policy =>
                    policy.RequireClaim("VIPLevel"));
            });

            // 5. 配置 CORS
            self.Builder.Services.AddCors(options =>
            {
                options.AddPolicy("GameClient", builder =>
                {
                    builder.WithOrigins(
                            "https://game.example.com",
                            "https://cdn.game.example.com"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // 6. 注册游戏服务
            self.Builder.Services.AddSingleton<IPlayerService, PlayerService>();
            self.Builder.Services.AddSingleton<IGuildService, GuildService>();
            self.Builder.Services.AddScoped<IAuthService, AuthService>();
            self.Builder.Services.AddScoped<IGameRepository, GameRepository>();

            Log.Info($"[HTTP] 游戏服务配置完成: Scene {self.Scene.SceneConfigId}");

            await FTask.CompletedTask;
        }
    }
}
```

### 2. 应用配置（OnConfigureHttpApplication）

```csharp
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Builder;
using System;

namespace Server.Hotfix
{
    public class GameHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
    {
        protected override async FTask Handler(OnConfigureHttpApplication self)
        {
            var app = self.Application;

            // 1. CORS（必须在认证之前）
            app.UseCors("GameClient");

            // 2. 请求日志中间件
            app.Use(async (context, next) =>
            {
                var requestId = Guid.NewGuid().ToString("N");
                context.Items["RequestId"] = requestId;

                var start = DateTime.UtcNow;
                var method = context.Request.Method;
                var path = context.Request.Path;
                var ip = context.Connection.RemoteIpAddress?.ToString();

                Log.Info($"[HTTP-{requestId}] {method} {path} - IP: {ip}");

                try
                {
                    await next.Invoke();

                    var duration = (DateTime.UtcNow - start).TotalMilliseconds;
                    var status = context.Response.StatusCode;
                    Log.Info($"[HTTP-{requestId}] {status} - {duration}ms");
                }
                catch (Exception e)
                {
                    Log.Error($"[HTTP-{requestId}] 异常: {e.Message}");
                    throw;
                }
            });

            // 3. 认证
            app.UseAuthentication();

            // 4. 授权
            app.UseAuthorization();

            // 5. 自定义响应头
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Game-Server", "Fantasy");
                context.Response.Headers.Add("X-Server-Version", "1.0.0");
                await next.Invoke();
            });

            Log.Info($"[HTTP] 游戏应用配置完成: Scene {self.Scene.SceneConfigId}");

            await FTask.CompletedTask;
        }
    }
}
```

### 3. 示例控制器

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Hotfix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly Scene _scene;
        private readonly IPlayerService _playerService;

        public PlayerController(Scene scene, IPlayerService playerService)
        {
            _scene = scene;
            _playerService = playerService;
        }

        /// <summary>
        /// 获取玩家信息（需要认证）
        /// </summary>
        [HttpGet("{playerId}")]
        [Authorize(Policy = "Player")]
        public async Task<IActionResult> GetPlayer(long playerId)
        {
            var player = await _playerService.GetPlayerAsync(playerId);
            if (player == null)
            {
                return NotFound(new { error = "Player not found" });
            }

            return Ok(new
            {
                playerId = player.Id,
                name = player.Name,
                level = player.Level,
                exp = player.Exp
            });
        }

        /// <summary>
        /// 登录接口（无需认证）
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var (success, token, playerId) = await _playerService.LoginAsync(
                request.Username, request.Password);

            if (!success)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }

            return Ok(new
            {
                token = token,
                playerId = playerId,
                expiresIn = 3600
            });
        }

        /// <summary>
        /// 管理员接口（需要 Admin 角色）
        /// </summary>
        [HttpPost("ban/{playerId}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> BanPlayer(long playerId, [FromBody] BanRequest request)
        {
            await _playerService.BanPlayerAsync(playerId, request.Reason, request.Duration);

            return Ok(new { message = "Player banned successfully" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class BanRequest
    {
        public string Reason { get; set; }
        public int Duration { get; set; } // 分钟
    }
}
```

---

## 最佳实践

### 1. 分离配置逻辑

如果配置逻辑复杂，建议创建单独的配置类：

```csharp
public static class JwtConfiguration
{
    public static void ConfigureJwt(this IServiceCollection services, string secretKey)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // JWT 配置...
            });
    }
}

public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        // 使用扩展方法
        self.Builder.Services.ConfigureJwt("your-secret-key");

        await FTask.CompletedTask;
    }
}
```

### 2. 环境相关配置

根据开发/生产环境使用不同配置：

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        if (self.Application.Environment.IsDevelopment())
        {
            // 开发环境：详细日志
            self.Application.Use(async (context, next) =>
            {
                Log.Debug($"Request Headers: {context.Request.Headers}");
                await next.Invoke();
            });
        }
        else
        {
            // 生产环境：HTTPS 重定向
            self.Application.UseHttpsRedirection();
        }

        await FTask.CompletedTask;
    }
}
```

### 3. 从配置文件加载设置

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        // 从 Scene 或配置系统加载设置
        var jwtSettings = LoadJwtSettings(self.Scene);
        var corsOrigins = LoadCorsOrigins(self.Scene);

        self.Builder.Services.AddAuthentication(/* use jwtSettings */);
        self.Builder.Services.AddCors(options =>
        {
            options.AddPolicy("Default", builder =>
            {
                builder.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
            });
        });

        await FTask.CompletedTask;
    }

    private JwtSettings LoadJwtSettings(Scene scene)
    {
        // 从数据库或配置文件加载
        return new JwtSettings { /* ... */ };
    }
}
```

### 4. 错误处理最佳实践

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error;

                // 根据异常类型返回不同状态码
                var statusCode = exception switch
                {
                    ArgumentException => 400,
                    UnauthorizedAccessException => 401,
                    InvalidOperationException => 409,
                    _ => 500
                };

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var result = new
                {
                    error = exception?.GetType().Name,
                    message = exception?.Message,
                    requestId = context.Items["RequestId"]
                };

                // 记录详细日志
                Log.Error($"[HTTP] 异常: {exception}");

                await context.Response.WriteAsJsonAsync(result);
            });
        });

        await FTask.CompletedTask;
    }
}
```

### 5. 性能监控中间件

```csharp
public class OnConfigureHttpApplicationHandler : AsyncEventSystem<OnConfigureHttpApplication>
{
    protected override async FTask Handler(OnConfigureHttpApplication self)
    {
        self.Application.Use(async (context, next) =>
        {
            var start = DateTime.UtcNow;

            await next.Invoke();

            var duration = (DateTime.UtcNow - start).TotalMilliseconds;

            // 慢请求告警
            if (duration > 1000)
            {
                Log.Warning($"[HTTP] 慢请求: {context.Request.Method} {context.Request.Path} - {duration}ms");
            }

            // 添加响应时间头
            context.Response.Headers.Add("X-Response-Time", $"{duration}ms");
        });

        await FTask.CompletedTask;
    }
}
```

---

## 常见问题

### Q1: 事件处理器没有被触发？

**检查以下几点：**

1. 确保 HTTP 服务器已配置并启动
2. 检查 `Fantasy.config` 中是否配置了 HTTP 网络监听
3. 确认事件处理器类在热更新程序集中（`Hotfix` 项目）
4. 查看日志中是否有异常信息

### Q2: 如何在事件中访问数据库？

```csharp
public class OnConfigureHttpServicesHandler : AsyncEventSystem<OnConfigureHttpServices>
{
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        // 从 Scene 获取数据库组件
        var dbComponent = self.Scene.GetComponent<DatabaseComponent>();

        // 加载配置
        var config = await dbComponent.LoadAsync<GameConfig>(configId);

        // 使用配置进行服务注册
        self.Builder.Services.AddSingleton(config);

        await FTask.CompletedTask;
    }
}
```

### Q3: 可以覆盖框架的默认配置吗？

**可以！** 用户配置会覆盖或追加到框架默认配置：

```csharp
// 框架默认：PropertyNamingPolicy = null
// 你可以覆盖：
self.MvcBuilder.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
```

### Q4: 中间件的执行顺序重要吗？

**非常重要！** 错误的顺序会导致功能失效：

```csharp
// ❌ 错误：认证在 CORS 之前会导致 OPTIONS 请求失败
app.UseAuthentication();
app.UseCors("AllowAll");

// ✅ 正确：CORS 应该在认证之前
app.UseCors("AllowAll");
app.UseAuthentication();
```

### Q5: 如何测试 HTTP API？

使用 curl 或 Postman：

```bash
# 登录获取 Token
curl -X POST http://localhost:5000/api/player/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"123456"}'

# 使用 Token 访问受保护接口
curl -X GET http://localhost:5000/api/player/12345 \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## 相关文档

- [01-ServerConfiguration.md](../../01-Server/01-ServerConfiguration.md) - Fantasy.config 配置文件详解
- [02-WritingStartupCode.md](../../01-Server/02-WritingStartupCode.md) - 编写启动代码
- [04-OnCreateScene.md](../../01-Server/04-OnCreateScene.md) - OnCreateScene 事件使用指南
- [04-Event.md](../CoreSystems/04-Event.md) - Event 系统使用指南

---

## 总结

通过 `OnConfigureHttpServices` 和 `OnConfigureHttpApplication` 两个事件，你可以完全控制 HTTP 服务器的行为：

✅ **服务配置阶段（OnConfigureHttpServices）**：
- 配置 MVC 选项（JSON、过滤器、验证）
- 注册认证和授权服务
- 注册依赖注入服务
- 配置 CORS 策略

✅ **中间件配置阶段（OnConfigureHttpApplication）**：
- 添加 CORS 中间件
- 添加认证和授权中间件
- 添加自定义中间件（日志、限流、监控）
- 配置异常处理

这两个事件设计符合 ASP.NET Core 的标准模式，让你可以利用整个 ASP.NET Core 生态系统的强大功能，同时保持 Fantasy Framework 的简洁性和高性能。
