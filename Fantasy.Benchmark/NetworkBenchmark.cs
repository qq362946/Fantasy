using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Platform.Console;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Benchmark;
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
public class NetworkBenchmark
{
    private static Scene _scene;
    private static Session _session;
    private readonly BenchmarkRequest _benchmarkRequest = new BenchmarkRequest();

    public static async FTask Initialize()
    {
        // 注册日志实例到框架中
        Log.Register(new ConsoleLog());
        // 初始化框架
        Entry.Initialize();
        // 执行StartUpdate方法
        Entry.StartUpdate();
        _scene = await Entry.CreateScene();
        // 创建远程连接
        _session = _scene.Connect("127.0.0.1:20000", NetworkProtocolType.WebSocket,
            () =>
            {
                Log.Debug("连接到目标服务器成功");
                var summary = BenchmarkRunner.Run<NetworkBenchmark>();
                Console.WriteLine(summary);
            },
            () => { Log.Debug("无法连接到目标服务器"); },
            () => { Log.Debug("与服务器断开连接"); }, false);
    }

    [Benchmark]
    public async FTask Call()
    {
        await _session.Call(_benchmarkRequest);
    }
}