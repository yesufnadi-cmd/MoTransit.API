using Microsoft.AspNetCore.Mvc;

namespace MohamedTransit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Success",
            Message = "Mohamed Transit API is up and running!"
        });
    }
}
