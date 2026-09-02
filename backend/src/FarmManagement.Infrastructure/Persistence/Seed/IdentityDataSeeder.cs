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
        new("Farm.Deactivate", "Deactivate farms.", "Farms"),
        new("FarmArea.View", "View farm areas.", "Farm Areas"),
        new("FarmArea.Create", "Create farm areas.", "Farm Areas"),
        new("FarmArea.Update", "Update farm areas.", "Farm Areas"),
        new("FarmArea.Activate", "Activate farm areas.", "Farm Areas"),
        new("FarmArea.Deactivate", "Deactivate farm areas.", "Farm Areas"),
        new("Crop.View", "View crops.", "Crops"),
        new("Crop.Create", "Create crops.", "Crops"),
        new("Crop.Update", "Update crops.", "Crops"),
        new("Crop.Activate", "Activate crops.", "Crops"),
        new("Crop.Deactivate", "Deactivate crops.", "Crops"),
        new("CropVariety.View", "View crop varieties.", "Crop Varieties"),
        new("CropVariety.Create", "Create crop varieties.", "Crop Varieties"),
        new("CropVariety.Update", "Update crop varieties.", "Crop Varieties"),
        new("CropVariety.Activate", "Activate crop varieties.", "Crop Varieties"),
        new("CropVariety.Deactivate", "Deactivate crop varieties.", "Crop Varieties"),
        new("CropLifecycleTemplate.View", "View crop lifecycle templates.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Create", "Create crop lifecycle templates.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Update", "Update crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Activate", "Activate crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Deactivate", "Deactivate crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("Plantation.View", "View crop plantations.", "Plantations"),
        new("Plantation.Create", "Create crop plantations.", "Plantations"),
        new("Plantation.Update", "Update crop plantations.", "Plantations"),
        new("Plantation.Activate", "Activate crop plantations.", "Plantations"),
        new("Plantation.Terminate", "Terminate crop plantations.", "Plantations"),
        new("PlantationEndReason.View", "View plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Create", "Create plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Update", "Update plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Activate", "Activate plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Deactivate", "Deactivate plantation end reasons.", "Plantation End Reasons")
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
            "Farm.Deactivate",
            "FarmArea.View",
            "FarmArea.Create",
            "FarmArea.Update",
            "FarmArea.Activate",
            "FarmArea.Deactivate",
            "Crop.View",
            "Crop.Create",
            "Crop.Update",
            "Crop.Activate",
            "Crop.Deactivate",
            "CropVariety.View",
            "CropVariety.Create",
            "CropVariety.Update",
            "CropVariety.Activate",
            "CropVariety.Deactivate",
            "CropLifecycleTemplate.View",
            "CropLifecycleTemplate.Create",
            "CropLifecycleTemplate.Update",
            "CropLifecycleTemplate.Activate",
            "CropLifecycleTemplate.Deactivate",
            "Plantation.View",
            "Plantation.Create",
            "Plantation.Update",
            "Plantation.Activate",
            "Plantation.Terminate",
            "PlantationEndReason.View",
            "PlantationEndReason.Create",
            "PlantationEndReason.Update",
            "PlantationEndReason.Activate",
            "PlantationEndReason.Deactivate"
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

    private static readonly IReadOnlyList<SeedCrop> SeedCrops =
    [
        new("GRAPES", "Grapes", "FRUIT", "PERENNIAL"),
        new("TOMATO", "Tomato", "VEGETABLE", "ANNUAL"),
        new("CHILI", "Chili", "VEGETABLE", "SEASONAL"),
        new("MANGO", "Mango", "FRUIT", "PERENNIAL"),
        new("BANANA", "Banana", "FRUIT", "PERENNIAL")
    ];

    private static readonly IReadOnlyList<SeedCropVariety> SeedCropVarieties =
    [
        new("GRAPES", "THOMPSON_SEEDLESS", "Thompson Seedless"),
        new("GRAPES", "SHARAD_SEEDLESS", "Sharad Seedless"),
        new("GRAPES", "SONAKA", "Sonaka"),
        new("GRAPES", "MANIK_CHAMAN", "Manik Chaman")
    ];

    private static readonly IReadOnlyList<SeedPlantationEndReason> SeedPlantationEndReasons =
    [
        new("HARVEST_COMPLETED", "Harvest Completed"),
        new("WEATHER_DISASTER", "Weather Disaster"),
        new("FLOOD", "Flood"),
        new("DROUGHT", "Drought"),
        new("CYCLONE", "Cyclone"),
        new("PEST_INFESTATION", "Pest Infestation"),
        new("DISEASE", "Disease"),
        new("CROP_FAILURE", "Crop Failure"),
        new("POOR_CROP_HEALTH", "Poor Crop Health"),
        new("SOIL_PROBLEM", "Soil Problem"),
        new("REPLANT_REQUIRED", "Replant Required"),
        new("FARMER_DECISION", "Farmer Decision"),
        new("OTHER", "Other")
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
        await SeedCropsAsync(cancellationToken);
        await SeedCropVarietiesAsync(cancellationToken);
        await SeedPlantationEndReasonsAsync(cancellationToken);
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

    private async Task SeedCropsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedCrop in SeedCrops)
        {
            var exists = await dbContext.Crops.AnyAsync(
                crop => crop.IsSystem && crop.OrganizationId == null && crop.Code == seedCrop.Code,
                cancellationToken);
            if (!exists)
            {
                dbContext.Crops.Add(new Crop(
                    organizationId: null,
                    code: seedCrop.Code,
                    name: seedCrop.Name,
                    cropType: seedCrop.CropType,
                    cropDurationType: seedCrop.DurationType,
                    isSystem: true));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCropVarietiesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedVariety in SeedCropVarieties)
        {
            var crop = await dbContext.Crops.SingleAsync(
                item => item.IsSystem && item.OrganizationId == null && item.Code == seedVariety.CropCode,
                cancellationToken);
            var exists = await dbContext.CropVarieties.AnyAsync(
                variety => variety.IsSystem && variety.OrganizationId == null &&
                           variety.CropId == crop.Id && variety.Code == seedVariety.Code,
                cancellationToken);
            if (!exists)
            {
                dbContext.CropVarieties.Add(new CropVariety(
                    organizationId: null,
                    cropId: crop.Id,
                    code: seedVariety.Code,
                    name: seedVariety.Name,
                    isSystem: true));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPlantationEndReasonsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedReason in SeedPlantationEndReasons)
        {
            var exists = await dbContext.PlantationEndReasons.AnyAsync(
                reason => reason.IsSystem && reason.OrganizationId == null && reason.Code == seedReason.Code,
                cancellationToken);
            if (!exists)
            {
                dbContext.PlantationEndReasons.Add(new PlantationEndReason(
                    organizationId: null,
                    code: seedReason.Code,
                    name: seedReason.Name,
                    isSystem: true));
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

    private sealed record SeedCrop(string Code, string Name, string CropType, string DurationType);

    private sealed record SeedCropVariety(string CropCode, string Code, string Name);

    private sealed record SeedPlantationEndReason(string Code, string Name);

    private sealed record InitialAdminConfiguration(string Email, string Password);
}
