using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public IActionResult MiniGame ([FromBody] Product product)
    {
        // 假设已经保存产品数据
        return Ok("Product created successfully");
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}