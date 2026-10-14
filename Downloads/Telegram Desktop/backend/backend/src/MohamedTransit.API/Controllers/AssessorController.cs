using Mapster;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Handlers.Assessor;
using MohamedTransit.API.DTO.NewFolder.Request;
using MohamedTransit.API.DTO.Assessor.Request;
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var customers = await _context.Set<Customer>()
            .Include(c => c.Documents)
            .Where(c => !c.IsVerified && c.RecordStatus == RecordStatus.Active)
            .OrderByDescending(c => c.CreateAt)
            .ToListAsync();

        var importerIds = customers
            .Where(customer => customer.UserId.HasValue)
            .Select(customer => customer.UserId!.Value)
            .Distinct()
            .ToArray();
        var activeServiceCounts = await _context.Set<Shipment>()
            .Where(shipment =>
                importerIds.Contains(shipment.ImporterId) &&
                shipment.RecordStatus == RecordStatus.Active &&
                shipment.Status != ShipmentStatus.Completed &&
                shipment.Status != ShipmentStatus.Rejected &&
                shipment.Status != ShipmentStatus.Cancelled &&
                shipment.Status != ShipmentStatus.Deliverd)
            .GroupBy(shipment => shipment.ImporterId)
            .Select(group => new { ImporterId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ImporterId, item => item.Count);

        var approvalRecords = customers.Select(customer => new
        {
            customer.Id,
            customer.BusinessName,
            customer.TINNumber,
            customer.BusinessLicense,
            customer.BusinessAddress,
            customer.ContactPerson,
            customer.ContactPhone,
            customer.ContactEmail,
            customer.BusinessType,
            customer.ImportLicense,
            customer.ImportLicenseExpiry,
            customer.IsVerified,
            customer.IsReturnedForRevision,
            customer.VerificationNotes,
            customer.CreateAt,
            ActiveServices = customer.UserId.HasValue &&
                             activeServiceCounts.TryGetValue(customer.UserId.Value, out var count)
                ? count
                : 0,
            Documents = customer.Documents
                .Where(document => document.RecordStatus == RecordStatus.Active)
                .Select(document => new
                {
                    document.Id,
                    document.OriginalFileName,
                    document.DocumentType,
                    document.IsRequired,
                    document.IsVerified,
                    document.ExpiryDate,
                    document.VerificationNotes,
                    document.CreateAt,
                    document.UpdatedAt
                })
                .ToList()
        });

        return HandleSuccessResponse(approvalRecords);
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.CustomerId <= 0)
            return BadRequest("A valid customer ID is required.");

        var customer = await _context.Set<Customer>()
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.RecordStatus == RecordStatus.Active);

        if (customer == null)
            return NotFound("Customer not found");

        if (request.ReturnForRevision)
        {
            if (request.IsApproved)
                return BadRequest("A customer cannot be approved and returned for revision in the same request.");
            if (string.IsNullOrWhiteSpace(request.Notes))
                return BadRequest("A revision note is required.");

            customer.ReturnForRevision(request.Notes.Trim());
        }
        else if (request.IsApproved)
        {
            var activeDocuments = customer.Documents
                .Where(document => document.RecordStatus == RecordStatus.Active)
                .ToList();
            if (activeDocuments.Any(document => !document.IsVerified || document.IsExpired))
                return BadRequest("Verify all active customer documents and resolve expired documents before approval.");

            customer.Verify(currentUserId.Value);
        }
        else
        {
            customer.RejectVerification(request.Notes);
        }

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(customer);
    }

    [HttpPut("VerifyCustomerDocument")]
    public async Task<IActionResult> VerifyCustomerDocument([FromBody] VerifyCustomerDocumentDto request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var document = await _context.Set<CustomerDocument>()
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item =>
                item.Id == request.DocumentId &&
                item.RecordStatus == RecordStatus.Active &&
                item.Customer != null &&
                item.Customer.RecordStatus == RecordStatus.Active &&
                !item.Customer.IsVerified);

        if (document == null)
            return NotFound("An active document for a pending customer was not found.");
        if (request.IsVerified && document.IsExpired)
            return BadRequest("An expired customer document cannot be verified.");

        document.SetVerification(request.IsVerified, currentUserId.Value, request.Notes?.Trim());
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new
        {
            document.Id,
            document.IsVerified,
            document.VerificationNotes,
            document.ExpiryDate
        });
    }

    [HttpGet("DownloadCustomerDocument")]
    public async Task<IActionResult> DownloadCustomerDocument([FromQuery] long documentId)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var document = await _context.Set<CustomerDocument>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == documentId &&
                item.RecordStatus == RecordStatus.Active &&
                item.Customer != null &&
                item.Customer.RecordStatus == RecordStatus.Active &&
                !item.Customer.IsVerified);

        if (document == null)
            return NotFound("An active document for a pending customer was not found.");
        if (string.IsNullOrWhiteSpace(document.FilePath) || !System.IO.File.Exists(document.FilePath))
            return NotFound("The customer document file was not found on the server.");

        return PhysicalFile(
            document.FilePath,
            string.IsNullOrWhiteSpace(document.MimeType) ? "application/octet-stream" : document.MimeType,
            System.IO.Path.GetFileName(document.OriginalFileName));
    }

    [HttpGet("GetAssessmentDocuments")]
    public async Task<IActionResult> GetAssessmentDocuments()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var documents = await _context.Set<ServiceDocument>()
            .AsNoTracking()
            .Where(document =>
                document.RecordStatus == RecordStatus.Active &&
                document.Shipment != null &&
                document.Shipment.RecordStatus == RecordStatus.Active)
            .OrderByDescending(document => document.CreateAt)
            .Select(document => new
            {
                document.Id,
                document.OriginalFileName,
                document.DocumentType,
                ServiceId = document.ShipmentId,
                ServiceNumber = document.Shipment!.TrackingNumber,
                ServiceStageId = document.ShipmentStageId,
                UploadedByUser = document.UploadedByUser == null
                    ? null
                    : new
                    {
                        document.UploadedByUser.FirstName,
                        document.UploadedByUser.LastName,
                        document.UploadedByUser.Username,
                        document.UploadedByUser.Email
                    },
                document.CreateAt,
                document.IsVerified,
                document.FileSizeBytes,
                document.Description
            })
            .ToListAsync();

        return HandleSuccessResponse(documents);
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var pending = await _context.Set<Shipment>()
            .AsNoTracking()
            .Where(s => s.Status == ShipmentStatus.Submitted && s.RecordStatus == RecordStatus.Active)
            .OrderByDescending(s => s.CreateAt)
            .Select(s => new
            {
                s.Id,
                s.TrackingNumber,
                s.ImporterId,
                Importer = new
                {
                    s.Importer.Id,
                    s.Importer.FirstName,
                    s.Importer.LastName,
                    s.Importer.Username,
                    s.Importer.Email,
                    s.Importer.Phone
                },
                Customer = _context.Set<Customer>()
                    .Where(customer => customer.UserId == s.ImporterId &&
                                       customer.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(customer => customer.CreateAt)
                    .Select(customer => new
                    {
                        customer.Id,
                        customer.BusinessName,
                        customer.TINNumber,
                        customer.BusinessLicense,
                        customer.ImportLicense,
                        customer.ImportLicenseExpiry,
                        customer.BusinessAddress,
                        customer.City,
                        customer.State,
                        customer.PostalCode,
                        customer.ContactPerson,
                        customer.ContactPhone,
                        customer.ContactEmail,
                        customer.BusinessType,
                        customer.IsVerified,
                        Documents = customer.Documents
                            .Where(document => document.RecordStatus == RecordStatus.Active)
                            .Select(document => new
                            {
                                document.Id,
                                document.OriginalFileName,
                                document.DocumentType,
                                document.Description,
                                document.IsRequired,
                                document.IsVerified,
                                document.ExpiryDate,
                                document.VerificationNotes,
                                document.CreateAt,
                                document.UpdatedAt
                            })
                            .ToList()
                    })
                    .FirstOrDefault(),
                s.Description,
                s.Mode,
                s.AssignedHub,
                s.Status,
                s.RouteCategory,
                s.DeclaredValue,
                s.TaxCategory,
                s.CountryOfOrigin,
                s.RiskLevel,
                s.Origin,
                s.Destination,
                s.AssignedCaseExecutorId,
                AssignedCaseExecutor = s.AssignedCaseExecutor == null
                    ? null
                    : new
                    {
                        s.AssignedCaseExecutor.Id,
                        s.AssignedCaseExecutor.FirstName,
                        s.AssignedCaseExecutor.LastName,
                        s.AssignedCaseExecutor.Username
                    },
                s.ServiceType,
                s.AssignedAssessorId,
                s.AssignmentNotes,
                s.CreateAt,
                s.UpdatedAt,
                Documents = s.Documents
                    .Where(document => document.RecordStatus == RecordStatus.Active)
                    .Select(document => new
                    {
                        document.Id,
                        document.OriginalFileName,
                        document.DocumentType,
                        document.Description,
                        document.FileSizeBytes,
                        document.MimeType,
                        document.IsRequired,
                        document.IsVerified,
                        document.VerificationNotes,
                        document.CreateAt,
                        document.UpdatedAt
                    })
                    .ToList(),
                Stages = s.Stages
                    .Select(stage => new
                    {
                        stage.Id,
                        stage.Stage,
                        stage.Status,
                        stage.CreateAt,
                        stage.UpdatedAt
                    })
                    .ToList()
            })
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ShipmentId <= 0)
            return BadRequest("A valid shipment ID is required.");

        var service = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s =>
                s.Id == request.ShipmentId &&
                s.RecordStatus == RecordStatus.Active);

        if (service == null)
            return NotFound("Service not found");
        if (service.Status != ShipmentStatus.Submitted)
            return Conflict("Only submitted services can be reviewed.");
        if (!request.IsApproved && string.IsNullOrWhiteSpace(request.ReviewNotes))
            return BadRequest("Review notes are required when rejecting a service.");

        if (request.IsApproved)
        {
            service.UpdateStatus(ShipmentStatus.Approved);
            service.AssignAssessor(currentUserId.Value);
        }
        else
        {
            service.UpdateStatus(ShipmentStatus.Rejected);
        }

        if (!string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            var reviewComment = ServiceMessage.Create(
                "Service Review",
                request.ReviewNotes.Trim(),
                MessageType.System,
                service.Id,
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var query = _context.Set<Shipment>()
            .Include(s => s.Importer)
            .Include(s => s.AssignedCaseExecutor)
            .Include(s => s.Stages)
            .Where(s =>
                s.AssignedAssessorId == currentUserId.Value &&
                s.RecordStatus == RecordStatus.Active);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (type.HasValue)
            query = query.Where(s => s.ServiceType == type.Value);

        var services = await query
            .OrderByDescending(s => s.CreateAt)
            .ToListAsync();

        return HandleSuccessResponse(services);
    }

    /// <summary>
    /// Get all active service records for the assessor service register.
    /// </summary>
    [HttpGet("GetActiveServicesForRegister")]
    public async Task<IActionResult> GetActiveServicesForRegister(CancellationToken cancellationToken)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var services = await _context.Set<Shipment>()
            .AsNoTracking()
            .Where(shipment => shipment.RecordStatus == RecordStatus.Active)
            .OrderByDescending(shipment => shipment.CreateAt)
            .Select(shipment => new
            {
                shipment.Id,
                shipment.TrackingNumber,
                shipment.ImporterId,
                Importer = new
                {
                    shipment.Importer.Id,
                    shipment.Importer.FirstName,
                    shipment.Importer.LastName,
                    shipment.Importer.Username
                },
                Customer = _context.Set<Customer>()
                    .Where(customer =>
                        customer.UserId == shipment.ImporterId &&
                        customer.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(customer => customer.CreateAt)
                    .Select(customer => new
                    {
                        customer.Id,
                        customer.BusinessName,
                        customer.TINNumber
                    })
                    .FirstOrDefault(),
                shipment.Description,
                shipment.Mode,
                shipment.Status,
                shipment.RiskLevel,
                shipment.AssignedCaseExecutorId,
                AssignedCaseExecutor = shipment.AssignedCaseExecutor == null
                    ? null
                    : new
                    {
                        shipment.AssignedCaseExecutor.Id,
                        shipment.AssignedCaseExecutor.FirstName,
                        shipment.AssignedCaseExecutor.LastName,
                        shipment.AssignedCaseExecutor.Username
                    },
                shipment.ServiceType,
                shipment.AssignedAssessorId,
                shipment.AssignmentNotes,
                shipment.Origin,
                shipment.Destination,
                shipment.CreateAt,
                shipment.UpdatedAt,
                shipment.RecordStatus
            })
            .ToListAsync(cancellationToken);

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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ShipmentId <= 0 || string.IsNullOrWhiteSpace(request.Feedback))
            return BadRequest("A valid shipment ID and feedback are required.");

        var service = await _context.Set<Shipment>()
            .FirstOrDefaultAsync(s =>
                s.Id == request.ShipmentId &&
                s.RecordStatus == RecordStatus.Active);

        if (service == null)
            return NotFound("Service not found");

        var feedback = ServiceMessage.Create(
            "Compliance Feedback",
            request.Feedback,
            MessageType.System,
            request.ShipmentId,
            currentUserId.Value,
            service.AssignedCaseExecutorId,
            isUrgent: true
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var pendingCustomerApprovalsCount = await _context.Set<Customer>()
            .CountAsync(c =>
                !c.IsVerified &&
                !c.IsReturnedForRevision &&
                c.RecordStatus == RecordStatus.Active);

        var returnedCustomersForRevisionCount = await _context.Set<Customer>()
            .CountAsync(c =>
                !c.IsVerified &&
                c.IsReturnedForRevision &&
                c.RecordStatus == RecordStatus.Active);

        var verifiedCustomersCount = await _context.Set<Customer>()
            .CountAsync(c => c.IsVerified && c.RecordStatus == RecordStatus.Active);

        var pendingCustomerDocumentsCount = await _context.Set<CustomerDocument>()
            .CountAsync(document =>
                !document.IsVerified &&
                document.RecordStatus == RecordStatus.Active &&
                document.Customer != null &&
                !document.Customer.IsVerified &&
                document.Customer.RecordStatus == RecordStatus.Active);

        var decisionWindowStart = DateTime.UtcNow.AddDays(-30);
        var recentCustomerDecisionDurations = await _context.Set<Customer>()
            .AsNoTracking()
            .Where(customer =>
                customer.VerifiedByUserId == currentUserId.Value &&
                customer.VerifiedAt.HasValue &&
                customer.VerifiedAt.Value >= decisionWindowStart)
            .Select(customer => new
            {
                customer.CreateAt,
                VerifiedAt = customer.VerifiedAt!.Value
            })
            .ToListAsync();
        var orderedDecisionDurations = recentCustomerDecisionDurations
            .Select(decision => Math.Max(0, (decision.VerifiedAt - decision.CreateAt).TotalDays))
            .OrderBy(days => days)
            .ToArray();
        decimal? medianCustomerDecisionDays = orderedDecisionDurations.Length switch
        {
            0 => null,
            var count when count % 2 == 1 => (decimal)Math.Round(orderedDecisionDurations[count / 2], 1),
            var count => (decimal)Math.Round(
                (orderedDecisionDurations[count / 2 - 1] + orderedDecisionDurations[count / 2]) / 2,
                1)
        };

        var pendingServiceIds = await _context.Set<Shipment>()
            .Where(s => s.Status == ShipmentStatus.Submitted && s.RecordStatus == RecordStatus.Active)
            .Select(s => s.Id)
            .ToListAsync();

        var openCasesCount = await _context.Set<Shipment>()
            .CountAsync(s =>
                s.AssignedAssessorId == currentUserId.Value &&
                s.RecordStatus == RecordStatus.Active &&
                (s.Status == ShipmentStatus.UnderReview ||
                 s.Status == ShipmentStatus.Approved ||
                 s.Status == ShipmentStatus.InProgress));

        var completedReviewsTodayCount = await _context.Set<Shipment>()
            .CountAsync(s => s.AssignedAssessorId == currentUserId.Value &&
                             s.UpdatedAt.HasValue && s.UpdatedAt.Value.Date == DateTime.UtcNow.Date);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var approvedCustomersThisMonth = await _context.Set<Customer>()
            .CountAsync(c =>
                c.VerifiedByUserId == currentUserId.Value &&
                c.VerifiedAt.HasValue &&
                c.VerifiedAt.Value >= monthStart &&
                c.VerifiedAt.Value < nextMonthStart);
        var approvedServicesThisMonth = await _context.Set<Shipment>()
            .CountAsync(s =>
                s.AssignedAssessorId == currentUserId.Value &&
                s.Status == ShipmentStatus.Approved &&
                s.UpdatedAt.HasValue &&
                s.UpdatedAt.Value >= monthStart &&
                s.UpdatedAt.Value < nextMonthStart);

        var complianceServiceIds = await _context.Set<ServiceMessage>()
            .Where(m => m.MessageType == MessageType.System &&
                        m.Subject.Contains("Compliance") &&
                        m.IsUrgent &&
                                m.ServiceId.HasValue &&
                                m.Service != null &&
                                m.Service.RecordStatus == RecordStatus.Active)
            .Select(m => m.ServiceId!.Value)
            .Distinct()
            .ToListAsync();
        var riskServiceIds = await _context.Set<Shipment>()
            .Where(s =>
                s.AssignedAssessorId == currentUserId.Value &&
                s.RecordStatus == RecordStatus.Active &&
                (s.Status == ShipmentStatus.UnderReview ||
                 s.Status == ShipmentStatus.Approved ||
                 s.Status == ShipmentStatus.InProgress) &&
                (s.RiskLevel == RiskLevel.Red || s.RiskLevel == RiskLevel.Yellow))
            .Select(s => s.Id)
            .ToListAsync();
        var flaggedStageServiceIds = await _context.Set<ServiceStageExecution>()
            .Where(stage =>
                stage.ShipmentId.HasValue &&
                stage.Shipment != null &&
                stage.Shipment.AssignedAssessorId == currentUserId.Value &&
                stage.Shipment.RecordStatus == RecordStatus.Active &&
                stage.RecordStatus == RecordStatus.Active &&
                (stage.Shipment.Status == ShipmentStatus.UnderReview ||
                 stage.Shipment.Status == ShipmentStatus.Approved ||
                 stage.Shipment.Status == ShipmentStatus.InProgress) &&
                (stage.IsBlocked ||
                 stage.RequiresCustomerAction ||
                 stage.Status == StageStatus.NeedsReview))
            .Select(stage => stage.ShipmentId!.Value)
            .Distinct()
            .ToListAsync();
        var servicesNeedingAttention = pendingServiceIds
            .Concat(complianceServiceIds)
            .Concat(riskServiceIds)
            .Concat(flaggedStageServiceIds)
            .ToHashSet();

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
            ReturnedCustomersForRevision = returnedCustomersForRevisionCount,
            VerifiedCustomers = verifiedCustomersCount,
            PendingCustomerDocuments = pendingCustomerDocumentsCount,
            MedianCustomerDecisionDays = medianCustomerDecisionDays,
            PendingServiceReviews = pendingServiceIds.Count,
            ServicesUnderOversight = openCasesCount,
            OpenCases = openCasesCount,
            NeedsAttention = pendingCustomerApprovalsCount + servicesNeedingAttention.Count,
            DueWithin48Hours = (int?)null,
            ClearedThisMonth = approvedCustomersThisMonth + approvedServicesThisMonth,
            CompletedReviewsToday = completedReviewsTodayCount,
            RecentCustomerApprovals = recentCustomerApprovals,
            RecentServiceReviews = recentServiceReviews
        };

        return HandleSuccessResponse(dashboard);
    }

    [HttpGet("GetAssessmentLogs")]
    public async Task<IActionResult> GetAssessmentLogs()
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var customerApprovals = await _context.Set<Customer>()
            .AsNoTracking()
            .Where(customer =>
                customer.VerifiedByUserId == currentUserId.Value &&
                customer.VerifiedAt.HasValue)
            .Select(customer => new AssessmentActivityRecord(
                $"customer-{customer.Id}",
                "Customer approved",
                customer.BusinessName,
                $"Customer {customer.BusinessName} was verified.",
                customer.VerifiedAt!.Value,
                "Customer approvals"))
            .ToListAsync();

        var serviceApprovals = await _context.Set<Shipment>()
            .AsNoTracking()
            .Where(shipment =>
                shipment.AssignedAssessorId == currentUserId.Value &&
                shipment.Status == ShipmentStatus.Approved &&
                shipment.UpdatedAt.HasValue)
            .Select(shipment => new AssessmentActivityRecord(
                $"service-{shipment.Id}",
                "Service approved",
                shipment.TrackingNumber,
                $"Service {shipment.TrackingNumber} was approved.",
                shipment.UpdatedAt!.Value,
                "Service reviews"))
            .ToListAsync();

        var serviceReviewFeedback = await _context.Set<ServiceMessage>()
            .AsNoTracking()
            .Where(message =>
                message.SenderUserId == currentUserId.Value &&
                message.Subject == "Service Review" &&
                message.Service != null &&
                message.Service.RecordStatus == RecordStatus.Active)
            .Select(message => new AssessmentActivityRecord(
                $"service-review-feedback-{message.Id}",
                "Service review feedback recorded",
                message.Service!.TrackingNumber,
                message.Content,
                message.CreatedDate,
                "Service reviews"))
            .ToListAsync();

        var activities = customerApprovals
            .Concat(serviceApprovals)
            .Concat(serviceReviewFeedback)
            .OrderByDescending(activity => activity.Timestamp)
            .Take(100)
            .ToList();

        return HandleSuccessResponse(activities);
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
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

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

    [HttpGet("GetServiceDetails/{id:long}")]
    public async Task<IActionResult> GetServiceDetails(long id)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var service = await _context.Shipments
            .AsNoTracking()
            .Where(shipment => shipment.Id == id && shipment.RecordStatus == RecordStatus.Active)
            .Select(shipment => new
            {
                shipment.Id,
                shipment.TrackingNumber,
                shipment.Description,
                shipment.Mode,
                shipment.ServiceType,
                shipment.Status,
                shipment.RouteCategory,
                shipment.DeclaredValue,
                shipment.TaxCategory,
                shipment.CountryOfOrigin,
                shipment.RiskLevel,
                shipment.Origin,
                shipment.Destination,
                shipment.AssignedHub,
                shipment.AssignmentNotes,
                shipment.CreateAt,
                shipment.UpdatedAt,
                Importer = new
                {
                    shipment.Importer.Id,
                    shipment.Importer.FirstName,
                    shipment.Importer.LastName,
                    shipment.Importer.Username,
                    shipment.Importer.Email,
                    shipment.Importer.Phone
                },
                Customer = _context.Customers
                    .Where(customer =>
                        customer.UserId == shipment.ImporterId &&
                        customer.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(customer => customer.CreateAt)
                    .Select(customer => new
                    {
                        customer.Id,
                        customer.BusinessName,
                        customer.TINNumber,
                        customer.BusinessAddress,
                        customer.ContactPerson,
                        customer.ContactPhone,
                        customer.ContactEmail
                    })
                    .FirstOrDefault(),
                AssignedCaseExecutor = shipment.AssignedCaseExecutor == null
                    ? null
                    : new
                    {
                        shipment.AssignedCaseExecutor.Id,
                        shipment.AssignedCaseExecutor.FirstName,
                        shipment.AssignedCaseExecutor.LastName,
                        shipment.AssignedCaseExecutor.Username
                    },
                AssignedAssessor = shipment.AssignedAssessor == null
                    ? null
                    : new
                    {
                        shipment.AssignedAssessor.Id,
                        shipment.AssignedAssessor.FirstName,
                        shipment.AssignedAssessor.LastName,
                        shipment.AssignedAssessor.Username
                    },
                Stages = shipment.Stages
                    .Where(stage => stage.RecordStatus == RecordStatus.Active)
                    .OrderBy(stage => stage.Stage)
                    .Select(stage => new
                    {
                        stage.Id,
                        stage.Stage,
                        stage.Status,
                        stage.RequiresCustomerAction,
                        stage.IsBlocked,
                        stage.BlockReason,
                        stage.StartedAt,
                        stage.CompletedAt
                    })
                    .ToList(),
                Documents = shipment.Documents
                    .Where(document => document.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(document => document.CreateAt)
                    .Select(document => new
                    {
                        document.Id,
                        document.OriginalFileName,
                        document.DocumentType,
                        document.Description,
                        document.IsRequired,
                        document.IsVerified,
                        document.CreateAt,
                        document.ShipmentStageId,
                        UploadedBy = document.UploadedByUser == null
                            ? null
                            : document.UploadedByUser.FirstName + " " + document.UploadedByUser.LastName
                    })
                    .ToList(),
                Assessments = _context.ServiceAssessments
                    .Where(assessment =>
                        assessment.ServiceId == shipment.Id &&
                        assessment.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(assessment => assessment.CreateAt)
                    .Select(assessment => new
                    {
                        assessment.Id,
                        assessment.Decision,
                        assessment.TaxAmount,
                        assessment.DutyAmount,
                        assessment.Notes,
                        assessment.CreateAt,
                        Assessor = assessment.Assessor == null
                            ? null
                            : assessment.Assessor.FirstName + " " + assessment.Assessor.LastName
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return service == null
            ? NotFound("Service not found.")
            : HandleSuccessResponse(service);
    }

    [HttpGet("GetServiceStages/{serviceId:long}")]
    public async Task<IActionResult> GetServiceStages(long serviceId)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (!await _context.Shipments.AnyAsync(shipment =>
                shipment.Id == serviceId &&
                shipment.RecordStatus == RecordStatus.Active))
            return NotFound("Service not found.");

        var stages = await _context.ServiceStageExecutions
            .AsNoTracking()
            .Where(stage =>
                stage.ShipmentId == serviceId &&
                stage.RecordStatus == RecordStatus.Active)
            .OrderBy(stage => stage.Stage)
            .Select(stage => new
            {
                stage.Id,
                stage.Stage,
                stage.Status,
                stage.Comments,
                stage.RequiresCustomerAction,
                stage.IsBlocked,
                stage.BlockReason,
                stage.StartedAt,
                stage.CompletedAt,
                Documents = stage.Documents
                    .Where(document => document.RecordStatus == RecordStatus.Active)
                    .Select(document => new
                    {
                        document.Id,
                        document.OriginalFileName,
                        document.DocumentType,
                        document.Description,
                        document.IsVerified,
                        document.CreateAt
                    })
                    .ToList()
            })
            .ToListAsync();

        return HandleSuccessResponse(stages);
    }

    [HttpPost("RecordAssessment")]
    public async Task<IActionResult> RecordAssessment([FromBody] RecordAssessmentRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ServiceId <= 0 ||
            string.IsNullOrWhiteSpace(request.Decision) ||
            request.TaxAmount < 0 ||
            request.DutyAmount < 0)
            return BadRequest("A service, decision, and non-negative tax and duty amounts are required.");
        var normalizedDecision = request.Decision.Trim().Replace(" ", string.Empty).ToLowerInvariant();
        var decision = normalizedDecision switch
        {
            "approved" => "Approved",
            "rejected" => "Rejected",
            "requirescustomeraction" => "RequiresCustomerAction",
            _ => null
        };
        if (decision == null)
            return BadRequest("Decision must be Approved, Rejected, or RequiresCustomerAction.");

        var service = await _context.Shipments
            .FirstOrDefaultAsync(shipment =>
                shipment.Id == request.ServiceId &&
                shipment.RecordStatus == RecordStatus.Active);
        if (service == null)
            return NotFound("Service not found.");

        var assessment = ServiceAssessment.Create(
            service.Id,
            currentUserId.Value,
            decision,
            request.TaxAmount,
            request.DutyAmount,
            request.Notes?.Trim());
        _context.ServiceAssessments.Add(assessment);

        var assessmentStage = await _context.ServiceStageExecutions
            .FirstOrDefaultAsync(stage =>
                stage.ShipmentId == service.Id &&
                stage.Stage == ShipmentStage.AssessmentandTaxPayment &&
                stage.RecordStatus == RecordStatus.Active);
        if (assessmentStage != null)
        {
            var requiresCustomerAction = decision == "RequiresCustomerAction";
            assessmentStage.UpdateStatus(
                requiresCustomerAction ? StageStatus.Pending : StageStatus.Completed,
                request.Notes?.Trim());
            assessmentStage.SetCustomerActionRequired(requiresCustomerAction);
        }

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new
        {
            assessment.Id,
            assessment.ServiceId,
            assessment.Decision,
            assessment.TaxAmount,
            assessment.DutyAmount,
            assessment.Notes,
            assessment.CreateAt
        });
    }

    [HttpGet("GetStageDiscussions/{serviceId:long}/{stageId:long}")]
    public async Task<IActionResult> GetStageDiscussions(long serviceId, long stageId)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (!await _context.ServiceStageExecutions.AnyAsync(stage =>
                stage.Id == stageId &&
                stage.ShipmentId == serviceId &&
                stage.RecordStatus == RecordStatus.Active &&
                stage.Shipment != null &&
                stage.Shipment.RecordStatus == RecordStatus.Active))
            return NotFound("Service stage not found.");

        var discussions = await _context.StageComments
            .AsNoTracking()
            .Where(comment =>
                comment.ServiceStageId == stageId &&
                comment.RecordStatus == RecordStatus.Active)
            .OrderBy(comment => comment.CreateAt)
            .Select(comment => new
            {
                comment.Id,
                comment.Comment,
                comment.CreateAt,
                Author = comment.CommentedByUser == null
                    ? null
                    : new
                    {
                        comment.CommentedByUser.Id,
                        comment.CommentedByUser.FirstName,
                        comment.CommentedByUser.LastName,
                        comment.CommentedByUser.Username
                    }
            })
            .ToListAsync();

        return HandleSuccessResponse(discussions);
    }

    [HttpPost("AddStageDiscussion")]
    public async Task<IActionResult> AddStageDiscussion([FromBody] AddStageDiscussionRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ServiceId <= 0 || request.StageId <= 0 || string.IsNullOrWhiteSpace(request.Comment))
            return BadRequest("A service, stage, and comment are required.");
        if (!await _context.ServiceStageExecutions.AnyAsync(stage =>
                stage.Id == request.StageId &&
                stage.ShipmentId == request.ServiceId &&
                stage.RecordStatus == RecordStatus.Active &&
                stage.Shipment != null &&
                stage.Shipment.RecordStatus == RecordStatus.Active))
            return NotFound("Service stage not found.");

        var comment = StageComment.Create(
            request.Comment.Trim(),
            request.StageId,
            currentUserId.Value);
        _context.StageComments.Add(comment);
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new
        {
            comment.Id,
            comment.ServiceStageId,
            comment.Comment,
            comment.CreateAt
        });
    }

    [HttpGet("GetConsignmentDetails/{serviceId:long}")]
    public async Task<IActionResult> GetConsignmentDetails(long serviceId)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();

        var service = await _context.Shipments
            .AsNoTracking()
            .Where(shipment =>
                shipment.Id == serviceId &&
                shipment.RecordStatus == RecordStatus.Active)
            .Select(shipment => new
            {
                shipment.Id,
                shipment.TrackingNumber,
                shipment.Description,
                shipment.Mode,
                shipment.ServiceType,
                shipment.RouteCategory,
                shipment.DeclaredValue,
                shipment.TaxCategory,
                shipment.CountryOfOrigin,
                shipment.Origin,
                shipment.Destination,
                Transports = _context.Transports
                    .Where(transport =>
                        transport.RecordStatus == RecordStatus.Active &&
                        transport.ServiceStageId.HasValue &&
                        _context.ServiceStageExecutions.Any(stage =>
                            stage.Id == transport.ServiceStageId.Value &&
                            stage.ShipmentId == shipment.Id &&
                            stage.RecordStatus == RecordStatus.Active))
                    .OrderBy(transport => transport.Id)
                    .Select(transport => new
                    {
                        transport.Id,
                        transport.FullName,
                        transport.PlateNumber,
                        transport.PhoneNumber,
                        transport.ProductAmount,
                        transport.ServiceStageId
                    })
                    .ToList(),
                Documents = shipment.Documents
                    .Where(document => document.RecordStatus == RecordStatus.Active)
                    .OrderByDescending(document => document.CreateAt)
                    .Select(document => new
                    {
                        document.Id,
                        document.OriginalFileName,
                        document.DocumentType,
                        document.Description,
                        document.FileSizeBytes,
                        document.CreateAt,
                        document.ShipmentStageId
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return service == null
            ? NotFound("Service not found.")
            : HandleSuccessResponse(service);
    }

    [HttpPost("NotifyImporter")]
    public async Task<IActionResult> NotifyImporter([FromBody] NotifyImporterRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ServiceId <= 0 ||
            string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("A service, subject, and message are required.");

        var service = await _context.Shipments
            .FirstOrDefaultAsync(shipment =>
                shipment.Id == request.ServiceId &&
                shipment.RecordStatus == RecordStatus.Active);
        if (service == null)
            return NotFound("Service not found.");

        var subject = request.Subject.Trim();
        var message = request.Message.Trim();
        var serviceMessage = ServiceMessage.Create(
            subject,
            message,
            MessageType.Direct,
            service.Id,
            currentUserId.Value,
            service.ImporterId,
            isUrgent: request.IsUrgent);
        _context.ServiceMessages.Add(serviceMessage);
        _context.Notifications.Add(Notification.Create(
            subject,
            message,
            NotificationType.Message,
            service.ImporterId,
            service.Id,
            isUrgent: request.IsUrgent));
        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new
        {
            serviceMessage.Id,
            serviceMessage.ServiceId,
            serviceMessage.Subject,
            serviceMessage.Content,
            serviceMessage.CreatedDate,
            RecipientUserId = service.ImporterId
        });
    }

    [HttpPost("EscalateService")]
    public async Task<IActionResult> EscalateService([FromBody] EscalateServiceRequest request)
    {
        var currentUserId = JwtHelper.GetCurrentUserId(_httpContextAccessor, _context);
        if (currentUserId == null)
            return Unauthorized("User not authenticated");
        if (!await IsAssessor(currentUserId.Value))
            return Forbid();
        if (request.ServiceId <= 0 || string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("A service and escalation reason are required.");

        var service = await _context.Shipments
            .FirstOrDefaultAsync(shipment =>
                shipment.Id == request.ServiceId &&
                shipment.RecordStatus == RecordStatus.Active);
        if (service == null)
            return NotFound("Service not found.");

        var reason = request.Reason.Trim();
        var message = string.IsNullOrWhiteSpace(request.Notes)
            ? reason
            : $"{reason}{Environment.NewLine}{request.Notes.Trim()}";
        var serviceMessage = ServiceMessage.Create(
            "Service escalation",
            message,
            MessageType.System,
            service.Id,
            currentUserId.Value,
            isUrgent: true,
            priority: "High");
        _context.ServiceMessages.Add(serviceMessage);

        var managers = await _context.UserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.User.RecordStatus == RecordStatus.Active &&
                userRole.Role.RecordStatus == RecordStatus.Active &&
                (userRole.Role.Name.Trim().ToLower() == "manager" ||
                 userRole.Role.Name.Trim().ToLower() == "superadmin" ||
                 userRole.Role.Name.Trim().ToLower() == "admin"))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync();
        foreach (var managerId in managers)
        {
            _context.Notifications.Add(Notification.Create(
                "Service escalation",
                message,
                NotificationType.SystemAlert,
                managerId,
                service.Id,
                isUrgent: true));
        }

        await _context.SaveChangesAsync();

        return HandleSuccessResponse(new
        {
            serviceMessage.Id,
            ServiceId = service.Id,
            service.TrackingNumber,
            serviceMessage.Subject,
            serviceMessage.Content,
            serviceMessage.CreatedDate,
            NotifiedManagers = managers.Count
        });
    }

    private async Task<bool> IsAssessor(long userId)
    {
        var allowedRoles = new[] { "assessor", "admin", "superadmin" };

        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(userRole =>
                userRole.UserId == userId &&
                userRole.User.RecordStatus == RecordStatus.Active &&
                userRole.Role.RecordStatus == RecordStatus.Active &&
                allowedRoles.Contains(userRole.Role.Name.Trim().ToLower()));
    }

    private sealed record AssessmentActivityRecord(
        string Id,
        string Type,
        string Reference,
        string Description,
        DateTime Timestamp,
        string Stage);
}
