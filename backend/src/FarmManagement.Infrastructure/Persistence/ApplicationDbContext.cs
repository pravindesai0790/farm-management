using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<FarmOwnershipType> FarmOwnershipTypes => Set<FarmOwnershipType>();

    public DbSet<Farm> Farms => Set<Farm>();

    public DbSet<FarmArea> FarmAreas => Set<FarmArea>();

    public DbSet<Crop> Crops => Set<Crop>();

    public DbSet<CropVariety> CropVarieties => Set<CropVariety>();

    public DbSet<CropLifecycleTemplate> CropLifecycleTemplates => Set<CropLifecycleTemplate>();

    public DbSet<CropLifecycleStage> CropLifecycleStages => Set<CropLifecycleStage>();

    public DbSet<PlantationEndReason> PlantationEndReasons => Set<PlantationEndReason>();

    public DbSet<CropPlantation> CropPlantations => Set<CropPlantation>();

    public DbSet<CropCycle> CropCycles => Set<CropCycle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
