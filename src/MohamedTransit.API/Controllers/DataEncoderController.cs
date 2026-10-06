using Mapster;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MohamedTransit.API.DTO.MOT.Request;
using MohamedTransit.Application.Commands.DataEncoder;
using MohamedTransit.API.DTO.MOT.Response;
using MohamedTransit.API.Helpers;
using MohamedTransit.Application.Commands.Shipment;
using MohamedTransit.Application.DTO;
using MohamedTransit.Application.Queries;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;


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
    // POST: api/v1/DataEncoder/services
    [HttpPost("Create services")]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new Application.Commands.DataEncoder.CreateServiceCommand
        {
            Reference = request.Reference,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        };

        // ሃንደለሩ የተፈጠረውን ሰርቪስ Id (long) ይመልሳል
        var serviceId = await _mediator.Send(command, cancellationToken);

        // የተመዘገበውን ሙሉ መረጃ (Data) ከነ Id-ው መመለስ
        return Ok(new
        {
            success = true,
            message = "Service created successfully as draft.",
            data = new
            {
                id = serviceId,
                reference = request.Reference,
                contactPerson = request.ContactPerson,
                email = request.Email,
                phone = request.Phone,
                notes = request.Notes
            }
        });

    }
    [HttpPut("services/{id}/service-type")]
    public async Task<IActionResult> UpdateServiceType(long id, [FromBody] UpdateServiceTypeRequest request)
    {
        var service = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return NotFound("Service not found");

        service.UpdateServiceType(request.ServiceType); // (ወይም የሚመለከተው ፕሮፐርቲ)
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new { Success = true, Message = "Service type updated successfully." });
    }
    [HttpPost("services/{id}/documents")]
    public async Task<IActionResult> UploadServiceDocuments(long id, [FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        var service = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service == null)
            return NotFound("Service not found");

        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                long timestampTicks = DateTime.UtcNow.Ticks;
                var uniqueFileName = $"{id}_{timestampTicks}_{Path.GetFileName(file.FileName)}";

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                var relativePath = $"/uploads/documents/{uniqueFileName}";
                
            }
        }
await _context.SaveChangesAsync(cancellationToken);

        return HandleSuccessResponse(new { Success = true, Message = "Documents uploaded successfully." });
    }
    [HttpPost("services/{id}/submit")]
    public async Task<IActionResult> SubmitService(long id)
    {
        var service = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (service == null) return NotFound("Service not found");

        service.Submit();

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new { Success = true, Message = "Service submitted successfully and locked for approval." });
    }

    // GET: api/v1/DataEncoder/services
    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAllServices(CancellationToken cancellationToken)
    {
        var query = new GetAllServicesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// </summary>
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// </summary>
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// </summary>
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// </summary>
    /// <summary>
    /// Get all documents across all services for the Document Centre
    /// </summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetAllDocuments(CancellationToken cancellationToken)
    {
        var documents = await _context.ServiceDocuments
            .Include(d => d.Shipment)
            .OrderByDescending(d => d.CreateAt)
            .ToListAsync(cancellationToken);

        var documentDtos = documents.Select(d => new
        {
            id = d.Id,
            documentName = d.FileName,
            documentType = d.DocumentType.ToString(),
            // Reference ፋንታ የ Shipmentን Id መጠቀም (ወይም በሞዴልዎ ያለውን ትክክለኛ ስም ማስገባት)
            serviceReference = d.Shipment != null ? d.Shipment.Id.ToString() : "SVC-0000",
            stage = "N/A", // ሞዴሉ ላይ StageName ከሌለ በጊዜያዊነት
            uploadedBy = "System", // ሞዴሉ ላይ UploadedByName ከሌለ
            uploadedDate = d.CreateAt.ToString("dd MMM yyyy, HH:mm"),
            status = "Active" // Status ከሌለ
        });

        return Ok(new { Success = true, Data = documentDtos });
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






