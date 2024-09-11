using Fantasy.Entitas;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
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
    public IActionResult Greet()
    {
        return Ok($"Hello from the Fantasy controller! _scene.SceneType:{_scene.SceneType} _scene.SceneType:{_scene.SceneConfigId}");
    }
}