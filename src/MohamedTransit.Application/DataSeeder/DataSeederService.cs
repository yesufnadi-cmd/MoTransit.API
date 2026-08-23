//using Microsoft.EntityFrameworkCore;

//using MohamedTransit.Domain.Entities;
//// የ DbContext ፎልደርህ namespace እዚህ ይገባል (ለምሳሌ: MohamedTransit.Infrastructure.Persistence)

//namespace MohamedTransit.Application.DataSeeder;

//public class DataSeederService
//{
//    private readonly DbContext _context; // እዚህ ላይ የአንተን ApplicationDbContext ተጠቀም

//    public DataSeederService(DbContext context)
//    {
//        _context = context;
//    }

//    public async Task SeedAsync()
//    {
//        // 1. Default Roles መፍጠር (ከሌሉ ብቻ)
//        if (!await _context.Set<Role>().AnyAsync())
//        {
//            var roles = new List<Role>
//            {
//                new Role { Id = Guid.NewGuid(), Name = "Admin" },
//                new Role { Id = Guid.NewGuid(), Name = "Importer" },
//                new Role { Id = Guid.NewGuid(), Name = "CaseExecutor" }
//            };

//            await _context.Set<Role>().AddRangeAsync(roles);
//            await _context.SaveChangesAsync();
//        }

//        // 2. Default System Admin መፍጠር (ከሌለ ብቻ)
//        if (!await _context.Set<User>().AnyAsync(u => u.Email == "admin@mohamedtransit.com"))
//        {
//            var adminRole = await _context.Set<Role>().FirstOrDefaultAsync(r => r.Name == "Admin");

//            if (adminRole != null)
//            {
//                var adminUser = new User
//                {
//                    Id = Guid.NewGuid(),
//                    FullName = "System Admin",
//                    Email = "admin@mohamedtransit.com",
//                    PasswordHash = "hashed_password_here", // የሐሽ ተደርጎ የተቀመጠ ፓስወርድ
//                    RoleId = adminRole.Id
//                };

//                await _context.Set<User>().AddAsync(adminUser);
//                await _context.SaveChangesAsync();
//            }
//        }
//   }
//}
