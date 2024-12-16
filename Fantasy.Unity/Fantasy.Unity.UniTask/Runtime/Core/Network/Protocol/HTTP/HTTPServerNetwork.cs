#if FANTASY_NET
using System.Net;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8604 // Possible null reference argument.

// ReSharper disable PossibleMultipleEnumeration

namespace Fantasy.Network.HTTP
{
    /// <summary>
    /// HTTP服务器
    /// </summary>
    public sealed class HTTPServerNetwork : ANetwork
    {
        /// <summary>
        /// 初始化入口
        /// </summary>
        /// <param name="networkTarget"></param>
        /// <param name="urls"></param>
        public void Initialize(NetworkTarget networkTarget, IEnumerable<string> urls)
        {
            base.Initialize(NetworkType.Server, NetworkProtocolType.HTTP, networkTarget);

            try
            {
                StartAsync(urls);
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5)
                {
                    throw new Exception($"CMD管理员中输入: netsh http add urlacl url=http://*:8080/ user=Everyone", e);
                }

                Log.Error(e);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void StartAsync(IEnumerable<string> urls)
        {
            var builder = WebApplication.CreateBuilder();
            // 配置日志级别为 Warning 或更高
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            // 将Scene注册到 DI 容器中，传递给控制器
            builder.Services.AddSingleton(Scene);
            // 注册Scene同步过滤器
            builder.Services.AddScoped<SceneContextFilter>();
            // 注册控制器服务
            var addControllers = builder.Services.AddControllers()
                .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });
            foreach (var assembly in AssemblySystem.ForEachAssembly)
            {
                addControllers.AddApplicationPart(assembly);
            }

            var app = builder.Build();
            // 配置多个监听地址
            foreach (var url in urls)
            {
                app.Urls.Add(url);
            }

            // 启用开发者工具
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // 路由注册
            app.MapControllers();
            // 开启监听
            app.RunAsync();
            Log.Info($"SceneConfigId = {Scene.SceneConfigId} HTTPServer Listen {urls.FirstOrDefault()}");
        }

        /// <summary>
        /// 移除Channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void RemoveChannel(uint channelId)
        {
            throw new NotImplementedException();
        }
    }
}
#endif