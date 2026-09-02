using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmManagement.Infrastructure.Persistence.Seed;

public sealed class IdentityDataSeeder(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger)
{
    private const string DevelopmentOrganizationName = "Demo Farm Organization";
    private const string DevelopmentOrganizationCode = "DEMO";

    private static readonly IReadOnlyList<SeedRole> SeedRoles =
    [
        new("SuperAdmin", "Platform-wide administrator."),
        new("OrganizationAdmin", "Administrator for an organization."),
        new("FarmManager", "Manager of farm operations."),
        new("Supervisor", "Supervisor of farm work."),
        new("Worker", "Farm worker.")
    ];

    private static readonly IReadOnlyList<SeedPermission> SeedPermissions =
    [
        new("Users.View", "View users.", "Administration"),
        new("Users.Create", "Create users.", "Administration"),
        new("Users.Update", "Update users.", "Administration"),
        new("Users.Activate", "Activate users.", "Administration"),
        new("Users.Deactivate", "Deactivate users.", "Administration"),
        new("Users.Unlock", "Unlock users.", "Administration"),
        new("Users.ManageRoles", "Manage user roles.", "Administration"),
        new("Roles.View", "View roles.", "Roles"),
        new("Roles.Create", "Create roles.", "Roles"),
        new("Roles.Update", "Update roles.", "Roles"),
        new("Roles.Activate", "Activate roles.", "Roles"),
        new("Roles.Deactivate", "Deactivate roles.", "Roles"),
        new("Roles.ManagePermissions", "Manage role permissions.", "Roles"),
        new("Permissions.View", "View permissions.", "Permissions"),
        new("Farm.View", "View farms.", "Farms"),
        new("Farm.Create", "Create farms.", "Farms"),
        new("Farm.Update", "Update farms.", "Farms"),
        new("Farm.Activate", "Activate farms.", "Farms"),
        new("Farm.Deactivate", "Deactivate farms.", "Farms")
    ];

    private static readonly IReadOnlySet<string> OrganizationAdminPermissions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Users.View",
            "Users.Create",
            "Users.Update",
            "Users.Activate",
            "Users.Deactivate",
            "Users.Unlock",
            "Users.ManageRoles",
            "Roles.View",
            "Permissions.View",
            "Farm.View",
            "Farm.Create",
            "Farm.Update",
            "Farm.Activate",
            "Farm.Deactivate"
        };

    private static readonly IReadOnlyList<SeedFarmOwnershipType> SeedFarmOwnershipTypes =
    [
        new("OWNED", "Owned"),
        new("LEASED", "Leased"),
        new("RENTED", "Rented"),
        new("MANAGED", "Managed"),
        new("OTHER", "Other")
    ];

    private static readonly IReadOnlyList<SeedUnit> SeedUnits =
    [
        new("ACRE", "Acre", "ac", UnitCategory.Area, "SQUARE_METER", 4046.8564224m, 10),
        new("HECTARE", "Hectare", "ha", UnitCategory.Area, "SQUARE_METER", 10000m, 20),
        new("SQUARE_METER", "Square Meter", "m²", UnitCategory.Area, "SQUARE_METER", 1m, 30),
        new("SQUARE_FEET", "Square Feet", "ft²", UnitCategory.Area, "SQUARE_METER", 0.09290304m, 40),
        new("KILOGRAM", "Kilogram", "kg", UnitCategory.Weight, "KILOGRAM", 1m, 10),
        new("GRAM", "Gram", "g", UnitCategory.Weight, "KILOGRAM", 0.001m, 20),
        new("TON", "Ton", "t", UnitCategory.Weight, "KILOGRAM", 1000m, 30),
        new("QUINTAL", "Quintal", "q", UnitCategory.Weight, "KILOGRAM", 100m, 40),
        new("LITER", "Liter", "L", UnitCategory.Volume, "LITER", 1m, 10),
        new("MILLILITER", "Milliliter", "mL", UnitCategory.Volume, "LITER", 0.001m, 20),
        new("METER", "Meter", "m", UnitCategory.Length, "METER", 1m, 10),
        new("CENTIMETER", "Centimeter", "cm", UnitCategory.Length, "METER", 0.01m, 20),
        new("FOOT", "Foot", "ft", UnitCategory.Length, "METER", 0.3048m, 30),
        new("NUMBER", "Number", "#", UnitCategory.Count, "NUMBER", 1m, 10),
        new("PIECE", "Piece", "pc", UnitCategory.Count, "NUMBER", 1m, 20),
        new("PLANT", "Plant", "plant", UnitCategory.Count, "NUMBER", 1m, 30)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var initialAdmin = ReadInitialAdminConfiguration();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var organization = await SeedOrganizationAsync(cancellationToken);
        var roles = await SeedRolesAsync(cancellationToken);
        var permissions = await SeedPermissionsAsync(cancellationToken);

        await SeedRolePermissionsAsync(roles, permissions, cancellationToken);
        await SeedUnitsAsync(cancellationToken);
        await SeedFarmOwnershipTypesAsync(cancellationToken);
        await SeedInitialSuperAdminAsync(organization, roles["SuperAdmin"], initialAdmin, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Identity data seeding completed.");
    }

    private async Task SeedUnitsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedUnit in SeedUnits)
        {
            var unitExists = await dbContext.Units
                .AnyAsync(
                    unit => unit.OrganizationId == null && unit.Code == seedUnit.Code,
                    cancellationToken);

            if (unitExists)
            {
                continue;
            }

            dbContext.Units.Add(new Unit(
                organizationId: null,
                code: seedUnit.Code,
                name: seedUnit.Name,
                symbol: seedUnit.Symbol,
                unitCategory: seedUnit.Category,
                baseUnitCode: seedUnit.BaseUnitCode,
                conversionFactor: seedUnit.ConversionFactor,
                isSystem: true,
                displayOrder: seedUnit.DisplayOrder));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFarmOwnershipTypesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedType in SeedFarmOwnershipTypes)
        {
            var exists = await dbContext.FarmOwnershipTypes
                .AnyAsync(item => item.Code == seedType.Code, cancellationToken);
            if (!exists)
            {
                dbContext.FarmOwnershipTypes.Add(new FarmOwnershipType(seedType.Code, seedType.Name));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Organization> SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(
                item => item.Code == DevelopmentOrganizationCode,
                cancellationToken);

        if (organization is not null)
        {
            return organization;
        }

        organization = new Organization(
            DevelopmentOrganizationName,
            DevelopmentOrganizationCode);

        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);
        return organization;
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new Dictionary<string, Role>(StringComparer.Ordinal);

        foreach (var seedRole in SeedRoles)
        {
            var role = await dbContext.Roles
                .SingleOrDefaultAsync(item => item.Name == seedRole.Name, cancellationToken);

            if (role is null)
            {
                role = new Role(seedRole.Name, seedRole.Description, isSystemRole: true);
                dbContext.Roles.Add(role);
            }
            else
            {
                role.MarkAsSystemRole();
            }

            roles.Add(seedRole.Name, role);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return roles;
    }

    private async Task<Dictionary<string, Permission>> SeedPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = new Dictionary<string, Permission>(StringComparer.Ordinal);

        foreach (var seedPermission in SeedPermissions)
        {
            var permission = await dbContext.Permissions
                .SingleOrDefaultAsync(item => item.Name == seedPermission.Name, cancellationToken);

            if (permission is null)
            {
                permission = new Permission(
                    seedPermission.Name,
                    seedPermission.Module,
                    seedPermission.Description);

                dbContext.Permissions.Add(permission);
            }

            permissions.Add(seedPermission.Name, permission);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return permissions;
    }

    private async Task SeedRolePermissionsAsync(
        IReadOnlyDictionary<string, Role> roles,
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken cancellationToken)
    {
        var existingAssignments = await dbContext.RolePermissions
            .AsNoTracking()
            .Select(rolePermission => new
            {
                rolePermission.RoleId,
                rolePermission.PermissionId
            })
            .ToListAsync(cancellationToken);

        var existingAssignmentKeys = existingAssignments
            .Select(item => (item.RoleId, item.PermissionId))
            .ToHashSet();

        foreach (var permission in permissions.Values)
        {
            AddRolePermissionIfMissing(
                roles["SuperAdmin"],
                permission,
                existingAssignmentKeys);
        }

        foreach (var permissionName in OrganizationAdminPermissions)
        {
            AddRolePermissionIfMissing(
                roles["OrganizationAdmin"],
                permissions[permissionName],
                existingAssignmentKeys);
        }
    }

    private void AddRolePermissionIfMissing(
        Role role,
        Permission permission,
        ISet<(Guid RoleId, Guid PermissionId)> existingAssignmentKeys)
    {
        if (!existingAssignmentKeys.Add((role.Id, permission.Id)))
        {
            return;
        }

        dbContext.RolePermissions.Add(new RolePermission(role.Id, permission.Id));
    }

    private async Task SeedInitialSuperAdminAsync(
        Organization organization,
        Role superAdminRole,
        InitialAdminConfiguration? initialAdmin,
        CancellationToken cancellationToken)
    {
        if (initialAdmin is null)
        {
            logger.LogWarning(
                "Initial admin credentials are not configured; the initial SuperAdmin was not created.");
            return;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Email.ToLower() == initialAdmin.Email,
                cancellationToken);

        if (user is null)
        {
            var existingSuperAdmin = await dbContext.UserRoles
                .AnyAsync(item => item.RoleId == superAdminRole.Id, cancellationToken);

            if (existingSuperAdmin)
            {
                logger.LogInformation(
                    "A SuperAdmin already exists; the configured initial admin was not created.");
                return;
            }

            var passwordHasher = new PasswordHasher<User>();
            var passwordHash = passwordHasher.HashPassword(null!, initialAdmin.Password);

            user = new User(
                organization.Id,
                "Initial",
                "Administrator",
                initialAdmin.Email,
                passwordHash);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Initial SuperAdmin created.");
        }

        var hasSuperAdminRole = await dbContext.UserRoles
            .AnyAsync(
                item => item.UserId == user.Id && item.RoleId == superAdminRole.Id,
                cancellationToken);

        if (!hasSuperAdminRole)
        {
            dbContext.UserRoles.Add(new UserRole(user.Id, superAdminRole.Id, DateTimeOffset.UtcNow));
        }
    }

    private InitialAdminConfiguration? ReadInitialAdminConfiguration()
    {
        var email = ReadConfigurationValue("InitialAdmin:Email", "INITIAL_ADMIN_EMAIL");
        var password = ReadConfigurationValue("InitialAdmin:Password", "INITIAL_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both INITIAL_ADMIN_EMAIL and INITIAL_ADMIN_PASSWORD must be configured together.");
        }

        return new InitialAdminConfiguration(email.Trim().ToLowerInvariant(), password);
    }

    private string? ReadConfigurationValue(string configurationKey, string environmentVariable)
    {
        return configuration[configurationKey]
            ?? configuration[environmentVariable]
            ?? Environment.GetEnvironmentVariable(environmentVariable);
    }

    private sealed record SeedRole(string Name, string Description);

    private sealed record SeedPermission(string Name, string Description, string Module);

    private sealed record SeedUnit(
        string Code,
        string Name,
        string Symbol,
        UnitCategory Category,
        string BaseUnitCode,
        decimal ConversionFactor,
        int DisplayOrder);

    private sealed record SeedFarmOwnershipType(string Code, string Name);

    private sealed record InitialAdminConfiguration(string Email, string Password);
}
