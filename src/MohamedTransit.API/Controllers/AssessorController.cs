using Mapster;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Handlers.Assessor;
using MohamedTransit.API.DTO.NewFolder.Request;
using MohamedTransit.API.Helpers;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

namespace MohamedTransit.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AssessorController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMediator _mediator;

    public AssessorController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IMediator mediator)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    /// <summary>
    /// Get customers pending approval
    /// </summary>
    [HttpGet("GetPendingCustomerApprovals")]
    public async Task<IActionResult> GetPendingCustomerApprovals()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var customers = await _context.Set<Customer>()
            .Include(c => c.User)
            .Include(c => c.CreatedByDataEncoder)
            .Include(c => c.Documents)
            .Where(c => !c.IsVerified && c.RecordStatus == RecordStatus.Active)
            .OrderByDescending(c => c.CreateAt)
            .ToListAsync();

        return HandleSuccessResponse(customers);
    }

    /// <summary>
    /// Approve or reject a customer
    /// </summary>
    [HttpPut("ApproveCustomer")]
    public async Task<IActionResult> ApproveCustomer([FromBody] ApproveCustomerDto request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId);

        if (customer == null)
            return NotFound("Customer not found");

        // Domain method በመጠቀም Verification status ማስተካከል (Private setter ኤረርን ለማስወገድ)
        if (request.IsApproved)
        {
            customer.Verify(currentUserId.Value);
        }
        else
        {
            customer.RejectVerification(request.Notes);
        }

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(customer);
    }

    /// <summary>
    /// Get service requests pending review
    /// </summary>
    [HttpGet("GetPendingServiceReviews")]
    public async Task<IActionResult> GetPendingServiceReviews()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var pending = await _context.Set<Shipment>()
            .Include(s => s.Importer)
            .Include(s => s.Stages)
            .Where(s => s.Status == ShipmentStatus.Submitted && s.RecordStatus == RecordStatus.Active)
            .OrderByDescending(s => s.CreateAt)
            .ToListAsync();

        return HandleSuccessResponse(pending);
    }

    /// <summary>
    /// Review and approve/reject a service request
    /// </summary>
    [HttpPut("ReviewService")]
    public async Task<IActionResult> ReviewService([FromBody] ShipmentReviewRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var serviceId = GetIdFromRequest(request);
        var service = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            return NotFound("Service not found");

        if (request.IsApproved)
        {
            service.UpdateStatus(ShipmentStatus.Approved);
            service.AssignAssessor(currentUserId.Value);
        }
        else
        {
            service.UpdateStatus(ShipmentStatus.Rejected);
        }

        if (!string.IsNullOrEmpty(request.ReviewNotes))
        {
            // CreatedByDataEncoderId ምትክ CreatedByUserId በመጠቀም ስህተቱን ማስተካከል
            var reviewComment = ServiceMessage.Create(
                "Service Review",
                request.ReviewNotes,
                MessageType.System,
                serviceId,
                currentUserId.Value,
                service.CreatedByUserId
            );

            _context.Set<ServiceMessage>().Add(reviewComment);
        }

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(service);
    }

    /// <summary>
    /// Get services under assessor oversight
    /// </summary>
    [HttpGet("GetServicesUnderOversight")]
    public async Task<IActionResult> GetServicesUnderOversight(
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] ServiceType? type = null)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var query = _context.Set<Shipment>()
            .Include(s => s.Importer)
            .Include(s => s.AssignedCaseExecutor)
            .Include(s => s.Stages)
            .Where(s => s.AssignedAssessorId == currentUserId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var services = await query
            .OrderByDescending(s => s.CreateAt)
            .ToListAsync();

        return HandleSuccessResponse(services);
    }

    /// <summary>
    /// Add compliance feedback to a service
    /// </summary>
    [HttpPost("AddComplianceFeedback")]
    public async Task<IActionResult> AddComplianceFeedback([FromBody] ComplianceFeedbackRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var serviceId = GetIdFromRequest(request);
        var service = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        if (service == null)
            return NotFound("Service not found");

        var feedback = ServiceMessage.Create(
            "Compliance Feedback",
            request.Feedback,
            MessageType.System,
            serviceId,
            currentUserId.Value,
            service.AssignedCaseExecutorId,
            null,
            true
        );

        _context.Set<ServiceMessage>().Add(feedback);
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(feedback);
    }

    /// <summary>
    /// Get assessor dashboard
    /// </summary>
    [HttpGet("GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var pendingCustomerApprovalsCount = await _context.Set<Customer>()
            .CountAsync(c => !c.IsVerified && c.RecordStatus == RecordStatus.Active);

        var pendingServiceReviewsCount = await _context.Set<Shipment>()
            .CountAsync(s => s.Status == ShipmentStatus.Submitted);

        var servicesUnderOversightCount = await _context.Set<Shipment>()
            .CountAsync(s => s.AssignedAssessorId == currentUserId.Value);

        var completedReviewsTodayCount = await _context.Set<Shipment>()
            .CountAsync(s => s.AssignedAssessorId == currentUserId.Value &&
                             s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date == DateTime.UtcNow.Date);

        var recentCustomerApprovals = await _context.Set<Customer>()
            .Include(c => c.User)
            .Where(c => c.VerifiedByUserId == currentUserId.Value)
            .OrderByDescending(c => c.VerifiedAt)
            .Take(5)
            .ToListAsync();

        var recentServiceReviews = await _context.Set<Shipment>()
            .Include(s => s.Importer)
            .Where(s => s.AssignedAssessorId == currentUserId.Value)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(5)
            .ToListAsync();

        var dashboard = new
        {
            PendingCustomerApprovals = pendingCustomerApprovalsCount,
            PendingServiceReviews = pendingServiceReviewsCount,
            ServicesUnderOversight = servicesUnderOversightCount,
            CompletedReviewsToday = completedReviewsTodayCount,
            RecentCustomerApprovals = recentCustomerApprovals,
            RecentServiceReviews = recentServiceReviews
        };

        return HandleSuccessResponse(dashboard);
    }

    /// <summary>
    /// Get compliance issues flagged
    /// </summary>
    [HttpGet("GetComplianceIssues")]
    public async Task<IActionResult> GetComplianceIssues()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");

        var issues = await _context.Set<ServiceMessage>()
            .Include(m => m.Service)
            .Include(m => m.SenderUser)
            .Where(m => m.MessageType == MessageType.System &&
                        m.Subject.Contains("Compliance") &&
                        m.IsUrgent)
            .OrderByDescending(m => m.Id)
            .ToListAsync();

        return HandleSuccessResponse(issues);
    }

    private async Task<bool> IsAssessor(long userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.UserRoles.Any(ur => ur.Role.Name == "Assessor") ?? false;
    }

    private static long GetIdFromRequest(object request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var type = request.GetType();
        var candidateNames = new[] { "ServiceId", "ShipmentId", "Id", "RequestId", "ServiceID", "ShipmentID" };

        foreach (var name in candidateNames)
        {
            var prop = type.GetProperty(name);
            if (prop == null) continue;
            var val = prop.GetValue(request);
            if (val == null) continue;
            try
            {
                return Convert.ToInt64(val);
            }
            catch
            {
                // ignore and continue
            }
        }

        throw new InvalidOperationException($"Request object of type {type.FullName} does not contain a recognized id property.");
    }
}
