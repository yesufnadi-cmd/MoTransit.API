using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MohamedTransit.API.Controllers;
using MohamedTransit.Application.DataSeeder;

namespace MohamedTransit.API.Controllers.UserAccount;

[ApiController]
[Route("api/v1/[controller]")]
public class SeederController : BaseController
{
    private readonly IDataSeederService _dataSeederService;
    private readonly IWebHostEnvironment _environment;

    public SeederController(IDataSeederService dataSeederService, IWebHostEnvironment environment)
    {
        _dataSeederService = dataSeederService;
        _environment = environment;
    }

    /// <summary>
    /// Seed the database with initial data
    /// </summary>
    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedDatabase()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            await _dataSeederService.SeedAsync();
            return HandleSuccessResponse(new { message = "Database seeded successfully" });
        }
        catch (Exception ex)
        {
            return HandleErrorResponse(ex);
        }
    }

    [HttpGet("credentials")]
    [AllowAnonymous]
    public IActionResult GetSeededCredentials()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var credentials = new
       {
           users = new[]
           {
              new { role = "Manager", username = "manager", password = "Manager123!", email = "manager@Mohamedtransit.com" }
            }
        };

        return HandleSuccessResponse(credentials);
    }
}
