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

    public SeederController(IDataSeederService dataSeederService)
    {
        _dataSeederService = dataSeederService;
    }

    /// <summary>
    /// Seed the database with initial data
    /// </summary>
    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedDatabase()
    {
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

    ///// <summary>
    ///// Get seeded user credentials for testing
    ///// </summary>
    [HttpGet("credentials")]
    [AllowAnonymous]
    public IActionResult GetSeededCredentials()
    {
        var credentials = new
       {
           users = new[]
           {
              new { role = "SuperAdmin", username = "superadmin", password = "Admin123!", email = "superadmin@transit.com" },
              new { role = "Manager", username = "manager", password = "Manager123!", email = "manager@transit.com" },
              new { role = "Assessor", username = "assessor", password = "Assessor123!", email = "assessor@transit.com" },
               new { role = "Case Executor", username = "caseexecutor", password = "Executor123!", email = "caseexecutor@transit.com" },
                new { role = "Data Encoder", username = "dataencoder", password = "Encoder123!", email = "dataencoder@transit.com" },
                new { role = "Customer", username = "customer", password = "Customer123!", email = "customer@transit.com" }
            }
        };

        return HandleSuccessResponse(credentials);
    }
}

