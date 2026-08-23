using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.Commands.UserAccount;
using MohamedTransit.Application.Helper;
using MohamedTransit.Application.Service;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

namespace MohamedTransit.Application.Handlers.UserAccount;

internal class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, OperationResult<Role>>
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ISession? _session => _httpContextAccessor.HttpContext?.Session;

    public CreateRoleCommandHandler(ApplicationDbContext context, PasswordService password, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _passwordService = password;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<OperationResult<Role>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var result = new OperationResult<Role>();

        var existingRole = await _context.Roles.FirstOrDefaultAsync(x => x.Name == request.Name);
        if (existingRole is not null)
        {
            result.AddError(ErrorCode.RecordFound, "Role already exist.");
            return result;
        }

        var role = Role.Create(request.Name, request.Description);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        if (request.Privileges.Count > 0)
        {
            foreach (var permission in request.Privileges)
            {
                await _context.AddAsync(new RolePrivilege() { PrivilegeId = permission, RoleId = role.Id });
            }
        }

        await _context.SaveChangesAsync();
        result.Payload = role;
        result.Message = "Operation success";

        return result;
    }
}
