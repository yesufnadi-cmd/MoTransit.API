using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MohamedTransit.API.Helpers;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;
using MohamedTransit.API.DTO.MOT.Response;


namespace MohamedTransit.API.Controllers.MOT;

[ApiController]
[Route("api/v1/[controller]")]
public class DataEncoderController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DataEncoderController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost("CreateCustomer")]
    public async Task<IActionResult> CreateCustomer([FromBody] MohamedTransit.API.DTO.MasterData.Request.CreateCustomerRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        // Check if user already exists
        var existingUser = await _context.Customers
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (existingUser != null)
            return BadRequest("User with this email or username already exists");

        // Create customer profile
        var customer = Customer.Create(
            request.BusinessName,
            request.TINNumber,
            request.BusinessLicense,
            request.BusinessAddress,
            request.City,
            request.State,
            request.PostalCode,
            request.ContactPerson,
            request.ContactPhone,
            request.ContactEmail,
            request.BusinessType,
            request.ImportLicense,
            request.ImportLicenseExpiry,
            request.UserId,
            currentUserId.Value
        );

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(customer);
    }

    /// <summary>
    /// Get all customers created by the data encoder
    /// </summary>
    [HttpGet("GetAllCustomers/{recordStatus}")]
    public async Task<IActionResult> GetAllCustomers(RecordStatus? recordStatus)
    {

        var query = new GetAllCustomersQuery { RecordStatus = recordStatus };
        var result = await _mediator.Send(query);
        var rolesList = result.Payload.Adapt<List<CustomerDetail>>();
        return result.IsError ? HandleErrorResponse(result.Errors) : HandleSuccessResponse(rolesList);

    }
    /// <summary>
    /// Update customer information before approval
    /// </summary>
    [HttpPut("UpdateCustomer")]
    public async Task<IActionResult> UpdateCustomer([FromBody] MohamedTransit.API.DTO.MOT.Request.UpdateCustomerRequest request)
    {
        var customer = await _context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.Id);

        if (customer == null)
            return NotFound("Customer not found or not created by you");


        customer.UpdateBusinessInfo(
            request.BusinessName,
            request.BusinessAddress,
            request.City,
            request.State,
            request.PostalCode,
            request.ContactPerson,
            request.ContactPhone,
            request.ContactEmail
        );

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(customer);
    }
   
    /// <summary>
    /// Get data encoder dashboard
    /// </summary>
    [HttpGet("GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");


        var dashboard = new MohamedTransit.API.DTO.MOT.Response.DataEncoderDashboardResponse
        {
            TotalCustomersCreated = await _context.Customers.CountAsync(c => c.CreatedByDataEncoderId == currentUserId.Value),
            PendingCustomerApprovals = await _context.Customers.CountAsync(c => c.CreatedByDataEncoderId == currentUserId.Value && !c.IsVerified),
            TotalServicesCreated = await _context.Shipments.CountAsync(s => s.CreatedByDataEncoderId == currentUserId.Value),
            PendingServiceApprovals = await _context.Shipments.CountAsync(s => s.CreatedByDataEncoderId == currentUserId.Value && s.Status == ShipmentStatus.Submitted),
            DraftServices = await _context.Shipments.CountAsync(s => s.CreatedByDataEncoderId == currentUserId.Value && s.Status == ShipmentStatus.Draft)
        };

        // Get recent activities
        var recentCustomersList = await _context.Customers
            .Include(c => c.User)
            .Where(c => c.CreatedByDataEncoderId == currentUserId.Value)
            .OrderByDescending(c => c.CreateAt)
            .Take(5)
            .ToListAsync();

        dashboard.RecentCustomers = recentCustomersList.Adapt<List<Customer>>();
        
    // DTO mapping

        var recentServicesList = await _context.Shipments
            .Include(s => s.Importer)
            .Where(s => s.CreatedByDataEncoderId == currentUserId.Value)
            .OrderByDescending(s => s.CreateAt)
            .Take(5)
            .ToListAsync();

        dashboard.RecentServices = recentServicesList.Adapt<List<ShipmentDetail>>(); // DTO mapping

        return HandleSuccessResponse(dashboard);
    }

    private async Task CreateServiceStages(long serviceId, ServiceType serviceType)
    {
        var stages = new List<ShipmentStage>
        {
            ShipmentStage.PrepaymentInvoice,
            ShipmentStage.DropRisk,
            ShipmentStage.DeliveryOrder,
            ShipmentStage.Inspection,
            ShipmentStage.Emergency,
            ShipmentStage.Clearance,
            ShipmentStage.Transportation,
            ShipmentStage.AssessmentandTaxPayment,
            ShipmentStage.WarehouseStatus,
            ShipmentStage.TransitPermission,
            ShipmentStage.Amendment,
            ShipmentStage.ExitandStoragePayment,



        };
        // Add unimodal-specific stages
        if (serviceType == ServiceType.Unimodal)
        {
            stages.Add(ShipmentStage.LocalPermission);
            stages.Add(ShipmentStage.Arrival);
        }

        foreach (var stage in stages)
        {
            var serviceStage = ServiceStageExecution.Create(serviceId, stage);
            _context.ShipmentStages.Add(serviceStage);
        }

        await _context.SaveChangesAsync();
    }

    private long? GetCurrentUserId()
    {
        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            return null;

        return 1; // This should be extracted from the JWT token
    }

    private async Task<bool> IsDataEncoder(long userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.UserRoles.Any(ur => ur.Role.Name == "DataEncoder") ?? false;
    }
}



