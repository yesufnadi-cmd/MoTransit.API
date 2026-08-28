//using System.Text.Json;

//using MediatR;

//using Microsoft.AspNetCore.Http;

//using MohamedTransit.Application;
//using MohamedTransit.Application.Helper;
//using MohamedTransit.Application.Service;
//using MohamedTransit.Domain.Data;

//using Transit.Domain;
//using Transit.Domain.Models.MOT;

//namespace Transit.Application;

//internal class ApproveCustomerCommandHandler : IRequestHandler<ApproveCustomerCommand, OperationResult<Customer>>
//{
//    private readonly ApplicationDbContext _context;
//    private readonly IHttpContextAccessor _httpContextAccessor;
//    private ISession? _session => _httpContextAccessor.HttpContext?.Session;
//    private readonly TokenHandlerService _tokenHandlerService;

//    public ApproveCustomerCommandHandler(
//        ApplicationDbContext context,
//        IHttpContextAccessor httpContextAccessor,
//        TokenHandlerService tokenHandlerService)
//    {
//        _context = context;
//        _httpContextAccessor = httpContextAccessor;
//        _tokenHandlerService = tokenHandlerService;
//    }

//    public async Task<OperationResult<Customer>> Handle(ApproveCustomerCommand request, CancellationToken cancellationToken)
//    {
//        var result = new OperationResult<Customer>();
//        var userName = GetCurrentUserName();
//        if (string.IsNullOrEmpty(userName))
//        {
//            userName = "";
//        }
//        long userId = 0;
//        if (userName.Length > 0)
//        {
//            var existingUsers = await _context.Users
//                      .FirstOrDefaultAsync(x => x.Username == userName);
//            if (existingUsers != null)
//                userId = existingUsers.Id;
//        }

//        // Verify customer exists
//        var customer = await _context.Customers
//            .Include(c => c.User)
//            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

//        if (customer == null)
//        {
//            result.AddError(ErrorCode.NotFound, "Customer not found.");
//            return result;
//        }

//        // Check if already verified
//        if (customer.IsVerified && request.IsApproved)
//        {
//            result.AddError(ErrorCode.RecordFound, "Customer is already verified.");
//            return result;
//        }

//        // Approve or reject customer
//        if (request.IsApproved)
//        {
//            customer.Verify(request.VerifiedByUserId, request.Notes);
//        }
//        else
//        {
//            // Handle rejection - update audit but don't verify
//            customer.UpdateAudit("System");
//        }

//        await _context.SaveChangesAsync(cancellationToken);

//        // Reload customer with relationships
//        var updatedCustomer = await _context.Customers
//            .Include(c => c.User)
//            .Include(c => c.VerifiedByUser)
//            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

//        if (updatedCustomer == null)
//        {
//            result.AddError(ErrorCode.NotFound, "Customer not found after approval.");
//            return result;
//        }

//        result.Payload = updatedCustomer;
//        result.Message = "Operation success";

//        var options = new JsonSerializerOptions
//        {
//            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
//            WriteIndented = true
//        };

//        return result;
//    }

//    private string? GetCurrentUserName()
//    {
//        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
//        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
//            return null;

//        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
//        var claims = _tokenHandlerService.GetClaims(token);
//        var userNameClaim = claims?.FirstOrDefault(c => c.Type == "userName");
//        return userNameClaim?.Value;
//    }
//}





