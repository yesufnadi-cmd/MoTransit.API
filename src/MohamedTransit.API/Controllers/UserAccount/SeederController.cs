using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MohamedTransit.Application.Helper; // Error, ErrorCode
using MohamedTransit.Application.DTO;
using MohamedTransit.Application.Commands.UserAccount;

namespace MohamedTransit.API.Controllers.UserAccount;

[ApiController]
[Route("api/v1/[controller]")]
public class SeederController : BaseController
{
    public SeederController()
    {
    }

    // =========================================================
    // SEED DATABASE
    // POST: api/v1/Seeder/seed
    // =========================================================
    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            // Build privileges list by reflecting controllers/actions similar to Privilege seeder
            var privileges = new List<PrivilegeDto>();
            var asm = System.Reflection.Assembly.GetExecutingAssembly();

            var controlleractionlist = asm.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
                .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public))
                .Where(m => !m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Any())
                .Select(x => new
                {
                    Controller = x.DeclaringType != null ? x.DeclaringType.Name : string.Empty,
                    Action = x.Name
                })
                .OrderBy(x => x.Controller)
                .ThenBy(x => x.Action)
                .ToList();

            foreach (var item in controlleractionlist)
            {
                var privilege = new PrivilegeDto
                {
                    Action = item.Controller.Replace("Controller", "") + "-" + item.Action,
                    Description = item.Controller.Replace("Controller", "")
                };
                privileges.Add(privilege);
            }

            var command = new PrivilegeSeeder(privileges);
            var result = await _mediator.Send(command);

            if (result == null) return BadRequest("An unexpected error occurred.");

            if (result.IsError)
                return HandleErrorResponse(result.Errors);

            return HandleSuccessResponse(new { Message = "Database seeded successfully." });
        }
        catch (Exception ex)
        {
            var error = new Error { Code = ErrorCode.ServerError, Message = ex.Message ?? "An unknown error occurred." };
            var errors = new List<Error> { error };

            return HandleErrorResponse(errors);
        }
    }

    // =========================================================
    // GET SEEDED CREDENTIALS (FOR TESTING ONLY)
    // GET: api/v1/Seeder/credentials
    // =========================================================
    [HttpGet("credentials")]
    [AllowAnonymous]
    public IActionResult GetSeededCredentials()
    {
        var credentials = new
        {
            Users = new[]
            {
                new { Role = "SuperAdmin", Username = "superadmin", Password = "Admin123!", Email = "superadmin@mohamedtransit.com" },
                new { Role = "Manager", Username = "manager", Password = "Manager123!", Email = "manager@mohamedtransit.com" },
                new { Role = "Assessor", Username = "assessor", Password = "Assessor123!", Email = "assessor@mohamedtransit.com" },
                new { Role = "Case Executor", Username = "caseexecutor", Password = "Executor123!", Email = "caseexecutor@mohamedtransit.com" },
                new { Role = "Data Encoder", Username = "dataencoder", Password = "Encoder123!", Email = "dataencoder@mohamedtransit.com" },
                new { Role = "Customer", Username = "customer", Password = "Customer123!", Email = "customer@mohamedtransit.com" }
            }
        };

        return HandleSuccessResponse(credentials);
    }
}
