using System.Threading;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(SceneContextFilter))]
public class HelloController : ControllerBase
{
    private readonly Scene _scene;
    
    /// <summary>
    /// 构造函数依赖注入
    /// </summary>
    /// <param name="scene"></param>
    public HelloController(Scene scene)
    {
        _scene = scene;
    }
    
    [HttpGet("greet")]
    public async FTask<IActionResult> Greet()
    {
        Log.Debug($"HelloController Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        return Ok($"Hello from the Fantasy controller! _scene.SceneType:{_scene.SceneType} _scene.SceneType:{_scene.SceneConfigId}");
    }
}