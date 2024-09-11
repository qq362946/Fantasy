using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
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
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}