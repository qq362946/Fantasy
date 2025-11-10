#if FANTASY_NET
using System;
using System.Net;
using System.Text;
using Fantasy.Assembly;
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
        /// <param name="bindIp"></param>
        /// <param name="port"></param>
        public void Initialize(NetworkTarget networkTarget, string bindIp, int port)
        {
            base.Initialize(NetworkType.Server, NetworkProtocolType.HTTP, networkTarget);

            try
            {
                StartAsync(bindIp, port);
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("CMD管理员中输入下面命令:");
                    sb.AppendLine($"netsh http add urlacl url=http://{bindIp}:{port}/ user=Everyone");
                    throw new Exception(sb.ToString(), e);
                }

                Log.Error(e);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void StartAsync(string bindIp, int port)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            // 将Scene注册到 DI 容器中，传递给控制器
            builder.Services.AddSingleton(Scene);
            // 注册Scene同步过滤器
            builder.Services.AddScoped<SceneContextFilter>();
            // 注册控制器服务
            var addControllers = builder.Services.AddControllers()
                .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });
            foreach (var assemblyManifest in AssemblyManifest.GetAssemblyManifest)
            {
                addControllers.AddApplicationPart(assemblyManifest.Assembly);
            }
            var app = builder.Build();
            var listenUrl = $"http://{bindIp}:{port}/";
            app.Urls.Add(listenUrl);
            // 启用开发者工具
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // 路由注册
            app.MapControllers();
            // 开启监听
            app.RunAsync();
            Log.Info($"SceneConfigId = {Scene.SceneConfigId} HTTPServer Listen {listenUrl}");
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