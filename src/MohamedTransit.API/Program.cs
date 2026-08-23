using Microsoft.EntityFrameworkCore;

using MohamedTransit.Api.Middleware;
using MohamedTransit.API;
using MohamedTransit.API.Services;
using MohamedTransit.Application;
using MohamedTransit.Application.Service;
using MohamedTransit.Application.Options;
using System.Text;
using MohamedTransit.Application.Services;
using MohamedTransit.Domain.Data;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers & HttpContext Accessor
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// 2. Session Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Application Services Registration
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<TokenHandlerService>();
builder.Services.AddScoped<PasswordService>(); // 👈 የጠፋው PasswordService እዚህ ተጨምሯል

// 3.a Bind and validate JWT settings early (fail fast on misconfiguration)
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<JwtSettings>, MohamedTransit.API.Validation.JwtSettingsValidator>();

// 4. OpenAPI / Scalar
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// 5. Application Layer
builder.Services.AddApplication();
builder.Services.AddScoped<EmailSenderService>();

// 6. Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// 7. Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 8. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientApplications", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 9. Database Migration & Admin Privilege Seeder Execution
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // ዳታቤዝ ሚግሬሽን ማካሄድ
    db.Database.Migrate();

    // Privileges እና SuperAdmin Role Seed ማድረግ
    AdminPrivilegeSeeder.Seed(scope.ServiceProvider);
}

// 10. Exception Middleware
app.UseExceptionHandler();

// 11. Scalar API Documentation
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Mohamed Transit API")
               .WithTheme(ScalarTheme.Purple);
    });
}

// 12. Middleware Pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowClientApplications");
app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.Run();
