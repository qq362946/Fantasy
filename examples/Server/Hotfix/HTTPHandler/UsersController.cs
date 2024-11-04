using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly Scene _scene;
    
    /// <summary>
    /// 构造函数依赖注入
    /// </summary>
    /// <param name="scene"></param>
    public UsersController(Scene scene)
    {
        _scene = scene;
    }
    
    [HttpGet("{userId}")]
    public IActionResult GetUser(int userId)
    {
        return Ok($"User ID: {userId}");
    }

    [HttpPost("register")]
    public IActionResult RegisterUser([FromBody] User user)
    {
        return Ok("User registered successfully");
    }
    
    [HttpGet("greet")]
    public IActionResult Greet()
    {
        Log.Debug($"HelloController Thread.CurrentThread.ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
        return Ok($"Hello from the Fantasy controller! _scene.SceneType:{_scene.SceneType} _scene.SceneType:{_scene.SceneConfigId}");
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}