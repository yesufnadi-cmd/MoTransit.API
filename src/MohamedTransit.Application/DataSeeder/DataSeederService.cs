using Microsoft.EntityFrameworkCore;
using MohamedTransit.Application.Service;
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;
namespace MohamedTransit.Application.DataSeeder;
public interface IDataSeederService
{
    Task SeedAsync();
}

public class DataSeederService : IDataSeederService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public DataSeederService(ApplicationDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task SeedAsync()
    {
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // 1. Seed privileges and save
        await SeedPrivilegesAsync();
        await _context.SaveChangesAsync();

        // 2. Seed roles and save (ይህ በጣም ወሳኝ ነው!)
        await SeedRolesAsync();
        await _context.SaveChangesAsync();

        // 3. Seed users and save
        await SeedUsersAsync();
        await _context.SaveChangesAsync();

        // 4. Assign privileges to roles and save
        await AssignPrivilegesToRolesAsync();
        await _context.SaveChangesAsync();

        // 5. Assign roles to users and save
        await AssignRolesToUsersAsync();
        await _context.SaveChangesAsync();
    }

    private async Task SeedPrivilegesAsync()
    {
        // Temporarily disable the check to force re-seeding
        // if (await _context.Privilege.AnyAsync())
        //     return;

        var privileges = new List<Privilege>
        {
            // Existing System Privileges
            Privilege.Create("User-Create", "Create new users"),
            Privilege.Create("User-Update", "Update user information"),
            Privilege.Create("User-Delete", "Delete users"),
            Privilege.Create("User-GetAll", "View all users"),
            Privilege.Create("User-GetById", "View user details"),
            Privilege.Create("User-Logout", "User logout"),

            Privilege.Create("Roles-Create", "Create new roles"),
            Privilege.Create("Roles-Update", "Update role information"),
            Privilege.Create("Roles-Delete", "Delete roles"),
            Privilege.Create("Roles-GetAll", "View all roles"),
            Privilege.Create("Role-GetById", "View role details"),

            Privilege.Create("Privilege-Create", "Create new privileges"),
            Privilege.Create("Privilege-Update", "Update privilege information"),
            Privilege.Create("Privilege-Delete", "Delete privileges"),
            Privilege.Create("Privilege-GetAll", "View all privileges"),

            // MOT Service Management Privileges
            Privilege.Create("Service-Create", "Create new services"),
            Privilege.Create("Service-Update", "Update service information"),
            Privilege.Create("Service-Delete", "Delete services"),
            Privilege.Create("Service-GetAll", "View all services"),
            Privilege.Create("Service-GetById", "View service details"),
            Privilege.Create("Service-UpdateStatus", "Update service status"),
            Privilege.Create("Service-Assign", "Assign services to users"),

            // MOT Customer Management Privileges
            Privilege.Create("Customer-Create", "Create new customers"),
            Privilege.Create("Customer-Update", "Update customer information"),
            Privilege.Create("Customer-Delete", "Delete customers"),
            Privilege.Create("Customer-GetAll", "View all customers"),
            Privilege.Create("Customer-GetById", "View customer details"),
            Privilege.Create("Customer-Approve", "Approve customer registrations"),
            Privilege.Create("Customer-CreateServiceRequest", "Create service requests"),

            // Document Management Privileges
            Privilege.Create("Document-Upload", "Upload documents"),
            Privilege.Create("Document-Download", "Download documents"),
            Privilege.Create("Document-Verify", "Verify documents"),
            Privilege.Create("Document-Delete", "Delete documents"),
            Privilege.Create("Document-GetAll", "View all documents"),

            // Messaging Privileges
            Privilege.Create("Message-Send", "Send messages"),
            Privilege.Create("Message-GetAll", "View all messages"),
            Privilege.Create("Message-GetById", "View message details"),

            // Notification Privileges
            Privilege.Create("Notification-Create", "Create notifications"),
            Privilege.Create("Notification-GetAll", "View all notifications"),
            Privilege.Create("Notification-MarkAsRead", "Mark notifications as read"),

            // Reporting Privileges
            Privilege.Create("Report-GetAll", "View all reports"),
            Privilege.Create("Report-Export", "Export reports"),

            // Dashboard Privileges
            Privilege.Create("Dashboard-View", "View dashboard"),
            Privilege.Create("Dashboard-Manager", "View manager dashboard"),
            Privilege.Create("Dashboard-Assessor", "View assessor dashboard"),
            Privilege.Create("Dashboard-CaseExecutor", "View case executor dashboard"),
            Privilege.Create("Dashboard-DataEncoder", "View data encoder dashboard"),
            Privilege.Create("Dashboard-GetAdminDashboardDataQuery", "Get admin dashboard data"),
            Privilege.Create("Dashboard-GetAdminDashboardDataSortQuery", "Get admin dashboard data with sorting"),
            Privilege.Create("Dashboard-GetOrganizationDashboardData", "Get organization dashboard data"),
            Privilege.Create("Dashboard-GetOrganizationDashboardDataSortQuery", "Get organization dashboard data with sorting"),
        };

        _context.Privileges.AddRange(privileges);
    }

    private async Task SeedRolesAsync()
    {
        // Temporarily disable the check to force re-seeding
        // if (await _context.Role.AnyAsync())
        //     return;

        var roles = new List<Role>
        {
            Role.Create("SuperAdmin", "Super Administrator with full system access"),
            Role.Create("Manager", "Manager with oversight and management capabilities"),
            Role.Create("Assessor", "Assessor responsible for customer verification and approval"),
            Role.Create("CaseExecutor", "Case Executor responsible for service execution"),
            Role.Create("DataEncoder", "Data Encoder responsible for data entry and service creation"),
            Role.Create("Customer", "Customer role for external users"),
        };

        _context.Roles.AddRange(roles);
    }

    private async Task SeedUsersAsync()
    {
        // Temporarily disable the check to force re-seeding
        // if (await _context.Users.AnyAsync())
        //     return;

        var users = new List<User>
        {
            // Super Admin
            User.CreateUser(
                "superadmin",
                "superadmin@Mohamedtransit.com",
                "Super",
                "Admin",
                "",
                "+1234567890",
                _passwordService.HashPassword("Admin123!"),
                true,
                AccountStatus.Approved
            ),

            // Manager
            User.CreateUser(
                "manager",
                "manager@Mohamedtransit.com",
                "Nadiya",
                "Manager",
                "",
                "+1234567891",
                _passwordService.HashPassword("Manager123!"),
                false,
                AccountStatus.Approved
            ),

            // Assessor
            User.CreateUser(
                "assessor",
                "assessor@Mohamedtransit.com",
                "Hulu",
                "Assessor",
                "",
                "+1234567892",
                _passwordService.HashPassword("Assessor123!"),
                false,
                AccountStatus.Approved
            ),

            // Case Executor
            User.CreateUser(
                "caseexecutor",
                "caseexecutor@Mohamedtransit.com",
                "Nebil",
                "Executor",
                "",
                "+1234567893",
                _passwordService.HashPassword("Executor123!"),
                false,
                AccountStatus.Approved
            ),

            // Data Encoder
            User.CreateUser(
                "dataencoder",
                "dataencoder@Mohamedtransit.com",
                "Selam",
                "Encoder",
                "",
                "+1234567894",
                _passwordService.HashPassword("Encoder123!"),
                false,
                AccountStatus.Approved
            ),

            // Customer
            User.CreateUser(
                "customer",
                "customer@Mohamedtransit.com",
                "Customer",
                "User",
                "",
                "+1234567895",
                _passwordService.HashPassword("Customer123!"),
                false,
                AccountStatus.Approved
            ),
        };

        _context.Users.AddRange(users);
    }

    private async Task AssignPrivilegesToRolesAsync()
    {
        // Temporarily disable the check to force re-assignment
        // if (await _context.RolePrivilege.AnyAsync())
        //     return;

        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
        var assessorRole = await _context.Roles .FirstOrDefaultAsync(r => r.Name == "Assessor");
        var caseExecutorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CaseExecutor");
        var dataEncoderRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "DataEncoder");
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");

        var allPrivileges = await _context.Privileges.ToListAsync();

        var rolePrivileges = new List<RolePrivilege>();

        // SuperAdmin gets all privileges
        foreach (var privilege in allPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(superAdminRole.Id, privilege.Id));
        }

        // Manager privileges
        var managerPrivileges = allPrivileges.Where(p =>
            p.Action.Contains("Service-") ||
            p.Action.Contains("Customer-") ||
            p.Action.Contains("Document-") ||
            p.Action.Contains("Message-") ||
            p.Action.Contains("Notification-") ||
            p.Action.Contains("Report-") ||
            p.Action.Contains("Dashboard-") ||
            p.Action.Contains("User-GetAll") ||
            p.Action.Contains("Role-GetAll")
        ).ToList();

        foreach (var privilege in managerPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(managerRole.Id, privilege.Id));
        }

        // Assessor privileges
        var assessorPrivileges = allPrivileges.Where(p =>
            p.Action.Contains("Customer-Approve") ||
            p.Action.Contains("Customer-GetAll") ||
            p.Action.Contains("Customer-GetById") ||
            p.Action.Contains("Document-Verify") ||
            p.Action.Contains("Document-GetAll") ||
            p.Action.Contains("Message-") ||
            p.Action.Contains("Notification-") ||
            p.Action.Contains("Dashboard-Assessor")
        ).ToList();

        foreach (var privilege in assessorPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(assessorRole.Id, privilege.Id));
        }

        // Case Executor privileges
        var caseExecutorPrivileges = allPrivileges.Where(p =>
            p.Action.Contains("Service-Update") ||
            p.Action.Contains("Service-UpdateStatus") ||
            p.Action.Contains("Service-GetAll") ||
            p.Action.Contains("Service-GetById") ||
            p.Action.Contains("Document-Upload") ||
            p.Action.Contains("Document-Download") ||
            p.Action.Contains("Document-GetAll") ||
            p.Action.Contains("Message-") ||
            p.Action.Contains("Notification-") ||
            p.Action.Contains("Dashboard-CaseExecutor")
        ).ToList();

        foreach (var privilege in caseExecutorPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(caseExecutorRole.Id, privilege.Id));
        }

        // Data Encoder privileges
        var dataEncoderPrivileges = allPrivileges.Where(p =>
            p.Action.Contains("Service-Create") ||
            p.Action.Contains("Service-GetAll") ||
            p.Action.Contains("Service-GetById") ||
            p.Action.Contains("Customer-Create") ||
            p.Action.Contains("Customer-GetAll") ||
            p.Action.Contains("Customer-GetById") ||
            p.Action.Contains("Document-Upload") ||
            p.Action.Contains("Document-GetAll") ||
            p.Action.Contains("Message-") ||
            p.Action.Contains("Notification-") ||
            p.Action.Contains("Dashboard-DataEncoder")
        ).ToList();

        foreach (var privilege in dataEncoderPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(dataEncoderRole.Id, privilege.Id));
        }

        // Customer privileges
        var customerPrivileges = allPrivileges.Where(p =>
            p.Action.Contains("Service-GetById") ||
            p.Action.Contains("Customer-GetById") ||
            p.Action.Contains("Customer-CreateServiceRequest") ||
            p.Action.Contains("Document-Download") ||
            p.Action.Contains("Message-Send") ||
            p.Action.Contains("Message-GetAll") ||
            p.Action.Contains("Notification-GetAll")
        ).ToList();

        foreach (var privilege in customerPrivileges)
        {
            rolePrivileges.Add(RolePrivilege.Create(customerRole.Id, privilege.Id));
        }

        _context.RolePrivileges.AddRange(rolePrivileges);
    }

    private async Task AssignRolesToUsersAsync()
    {
        if (await _context.UserRoles.AnyAsync())
            return;

        var superAdminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
        var managerUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "manager");
        var assessorUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "assessor");
        var caseExecutorUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "caseexecutor");
        var dataEncoderUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "dataencoder");
        var customerUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "customer");

        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        var managerRole = await _context.Roles      .FirstOrDefaultAsync(r => r.Name == "Manager");
        var assessorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Assessor");
        var caseExecutorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "CaseExecutor");
        var dataEncoderRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "DataEncoder");
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");

        var userRoles = new List<UserRole>
        {
            UserRole.Create(superAdminUser.Id, superAdminRole.Id),
            UserRole.Create(managerUser.Id, managerRole.Id),
            UserRole.Create(assessorUser.Id, assessorRole.Id),
            UserRole.Create(caseExecutorUser.Id, caseExecutorRole.Id),
            UserRole.Create(dataEncoderUser.Id, dataEncoderRole.Id),
            UserRole.Create(customerUser.Id, customerRole.Id),
        };

        _context.UserRoles.AddRange(userRoles);
    }
}
