using Microsoft.AspNetCore.Mvc;

namespace Fantasy;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetProductById(int id)
    {
        return Ok($"Product ID: {id}");
    }

    [HttpPost]
    public IActionResult CreateProduct([FromBody] Product product)
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