#if FANTASY_NET
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
                    sb.AppendLine("CMD管理员中输入下面其中一个命令，具体根据您是HTTPS或HTTP决定:");
                    sb.AppendLine($"HTTP请输入如下:netsh http add urlacl url=http://{bindIp}:{port}/ user=Everyone");
                    sb.AppendLine($"HTTPS请输入如下:netsh http add urlacl url=https://{bindIp}:{port}/ user=Everyone");
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
            var listenUrl = "";
            var app = builder.Build();
            // 检测当前路径下是否有证书文件
            var certificatePath = Path.Combine(AppContext.BaseDirectory, $"certificate{bindIp}{port}");
            if (Directory.Exists(certificatePath))
            {
                // 加载包含证书链的 PEM 文件
                var pemCertChain = File.ReadAllText(Path.Combine(certificatePath, "chain.pem"));
                var pemPrivateKey = File.ReadAllText(Path.Combine(certificatePath, "private-key.pem"));
                // 配置 HTTPS 监听并使用证书
                builder.WebHost.ConfigureKestrel(kestrelServerOptions =>
                {
                    kestrelServerOptions.ConfigureHttpsDefaults(https =>
                    {
                        https.ServerCertificate = X509Certificate2.CreateFromPem(pemCertChain, pemPrivateKey);
                    });
                });
                listenUrl = $"https://{bindIp}:{port}/";
                app.Urls.Add(listenUrl);
                app.UseHttpsRedirection();
            }
            else
            {
                // 不安全的HTTP地址
                listenUrl = $"http://{bindIp}:{port}/";
                app.Urls.Add(listenUrl);
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