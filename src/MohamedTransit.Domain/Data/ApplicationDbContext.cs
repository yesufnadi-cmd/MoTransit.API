using Microsoft.EntityFrameworkCore;

using MohamedTransit.Domain.Entities;

namespace MohamedTransit.Domain.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ==========================================
    // Shipment
    // ==========================================

    public DbSet<Shipment> Shipments => Set<Shipment>();


    // ==========================================
    // User Account Module
    // ==========================================

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Privilege> Privileges => Set<Privilege>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePrivilege> RolePrivileges => Set<RolePrivilege>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically load IEntityTypeConfiguration<T>
        // classes from the Domain assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);


        // ==========================================
        // UserRole
        // ==========================================

        modelBuilder.Entity<UserRole>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);


        // ==========================================
        // RolePrivilege
        // ==========================================

        modelBuilder.Entity<RolePrivilege>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<RolePrivilege>()
            .HasOne(x => x.Role)
            .WithMany(x => x.RolePrivileges)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePrivilege>()
            .HasOne(x => x.Privilege)
            .WithMany()
            .HasForeignKey(x => x.PrivilegeId)
            .OnDelete(DeleteBehavior.Restrict);


        // ==========================================
        // Shipment → User
        // ==========================================

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.ImporterId)
                .OnDelete(DeleteBehavior.NoAction);

           
        });
    }
}
