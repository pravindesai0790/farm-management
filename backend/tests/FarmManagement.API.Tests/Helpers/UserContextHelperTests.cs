using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.DTOs.LaborActivities;
using FarmManagement.Application.Interfaces;
using FarmManagement.Application.Interfaces.CropCycles;
using FarmManagement.Application.Interfaces.Crops;
using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Application.Interfaces.Organizations;
using FarmManagement.Application.Interfaces.Plantations;
using FarmManagement.Application.Interfaces.Roles;
using FarmManagement.Application.Interfaces.Users;
using Xunit;

namespace FarmManagement.API.Tests.Helpers;

public class UserContextHelperTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestOrganizationId = Guid.NewGuid();

    private static ClaimsPrincipal CreatePrincipal(
        string? userId = null,
        string? organizationId = null,
        bool useNameIdentifier = false,
        string? organizationScope = null,
        string? role = null)
    {
        var claims = new List<Claim>();

        if (userId is not null)
        {
            var claimType = useNameIdentifier ? ClaimTypes.NameIdentifier : JwtRegisteredClaimNames.Sub;
            claims.Add(new Claim(claimType, userId));
        }

        if (organizationId is not null)
        {
            claims.Add(new Claim(AuthorizationConstants.OrganizationIdClaimType, organizationId));
        }

        if (organizationScope is not null)
        {
            claims.Add(new Claim(AuthorizationConstants.OrganizationScopeClaimType, organizationScope));
        }

        if (role is not null)
        {
            claims.Add(new Claim(AuthorizationConstants.RoleClaimType, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void GetUserIds_WhenClaimsAreValid_ReturnsUserIdAndOrganizationId()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var (userId, organizationId) = UserContextHelper.GetUserIds(principal);

        Assert.Equal(TestUserId, userId);
        Assert.Equal(TestOrganizationId, organizationId);
    }

    [Fact]
    public void GetUserIds_WhenSubIsMissing_FallsBackToNameIdentifier()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString(), useNameIdentifier: true);

        var (userId, organizationId) = UserContextHelper.GetUserIds(principal);

        Assert.Equal(TestUserId, userId);
        Assert.Equal(TestOrganizationId, organizationId);
    }

    [Fact]
    public void GetUserIds_WhenUserIdIsMissing_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal(userId: null, TestOrganizationId.ToString());

        var ex = Assert.Throws<UnauthorizedAccessException>(() => UserContextHelper.GetUserIds(principal));
        Assert.Equal("The access token is invalid.", ex.Message);
    }

    [Fact]
    public void GetUserIds_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal("not-a-guid", TestOrganizationId.ToString());

        var ex = Assert.Throws<UnauthorizedAccessException>(() => UserContextHelper.GetUserIds(principal));
        Assert.Equal("The access token is invalid.", ex.Message);
    }

    [Fact]
    public void GetUserIds_WhenOrganizationIdIsMissing_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), organizationId: null);

        var ex = Assert.Throws<UnauthorizedAccessException>(() => UserContextHelper.GetUserIds(principal));
        Assert.Equal("The access token is invalid.", ex.Message);
    }

    [Fact]
    public void GetUserIds_WhenOrganizationIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), "invalid-org-id");

        var ex = Assert.Throws<UnauthorizedAccessException>(() => UserContextHelper.GetUserIds(principal));
        Assert.Equal("The access token is invalid.", ex.Message);
    }

    [Fact]
    public void GetAuthenticatedUserId_WhenClaimIsValid_ReturnsUserId()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), null);

        var userId = UserContextHelper.GetAuthenticatedUserId(principal);

        Assert.Equal(TestUserId, userId);
    }

    [Fact]
    public void GetAuthenticatedUserId_WhenClaimIsMissing_ThrowsUnauthorizedAccessException()
    {
        var principal = CreatePrincipal(null, null);

        var ex = Assert.Throws<UnauthorizedAccessException>(() => UserContextHelper.GetAuthenticatedUserId(principal));
        Assert.Equal("The access token is invalid.", ex.Message);
    }

    [Fact]
    public void CanManageAllOrganizations_WhenScopeIsAll_ReturnsTrue()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            organizationScope: AuthorizationConstants.AllOrganizationsScope);

        var result = UserContextHelper.CanManageAllOrganizations(principal);

        Assert.True(result);
    }

    [Fact]
    public void CanManageAllOrganizations_WhenScopeIsNotAll_ReturnsFalse()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            organizationScope: "limited");

        var result = UserContextHelper.CanManageAllOrganizations(principal);

        Assert.False(result);
    }

    [Fact]
    public void IsSuperAdmin_WhenRoleIsSuperAdmin_ReturnsTrue()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            role: AuthorizationConstants.SuperAdminRoleName);

        var result = UserContextHelper.IsSuperAdmin(principal);

        Assert.True(result);
    }

    [Fact]
    public void IsSuperAdmin_WhenRoleIsNotSuperAdmin_ReturnsFalse()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            role: "Farmer");

        var result = UserContextHelper.IsSuperAdmin(principal);

        Assert.False(result);
    }

    [Fact]
    public void GetUserContext_ForFarmActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<FarmActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForCropCycleActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<CropCycleActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForRoleAdministrationActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<RoleAdministrationActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForLaborActivityActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<LaborActivityActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForWorkerActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<WorkerActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForContractorActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<ContractorActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForLaborCategoryActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<LaborCategoryActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForDashboardActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<DashboardActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForLaborWageRateActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<LaborWageRateActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForEarningsActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<EarningsActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForPlantationActor_ReturnsExpectedValues()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var actor = UserContextHelper.GetUserContext<PlantationActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
    }

    [Fact]
    public void GetUserContext_ForUserAdministrationActor_PopulatesScopeFlag()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            organizationScope: AuthorizationConstants.AllOrganizationsScope);

        var actor = UserContextHelper.GetUserContext<UserAdministrationActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
        Assert.True(actor.CanManageAllOrganizations);
    }

    [Fact]
    public void GetUserContext_ForOrganizationActor_PopulatesScopeFlag()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            organizationScope: AuthorizationConstants.AllOrganizationsScope);

        var actor = UserContextHelper.GetUserContext<OrganizationActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
        Assert.True(actor.CanManageAllOrganizations);
    }

    [Fact]
    public void GetUserContext_ForCropActor_PopulatesSuperAdminFlag()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            role: AuthorizationConstants.SuperAdminRoleName);

        var actor = UserContextHelper.GetUserContext<CropActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
        Assert.True(actor.IsGlobalAdmin);
    }

    [Fact]
    public void GetUserContext_ForCropLifecycleTemplateActor_PopulatesSuperAdminFlag()
    {
        var principal = CreatePrincipal(
            TestUserId.ToString(),
            TestOrganizationId.ToString(),
            role: AuthorizationConstants.SuperAdminRoleName);

        var actor = UserContextHelper.GetUserContext<CropLifecycleTemplateActor>(principal);

        Assert.Equal(TestUserId, actor.UserId);
        Assert.Equal(TestOrganizationId, actor.OrganizationId);
        Assert.True(actor.IsGlobalAdmin);
    }

    [Fact]
    public void GetUserContext_WithCustomFactory_InvokesFactoryWithExtractedIds()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        var result = UserContextHelper.GetUserContext(principal, (uid, orgId) => $"{uid}:{orgId}");

        Assert.Equal($"{TestUserId}:{TestOrganizationId}", result);
    }

    [Fact]
    public void GetUserContext_WhenTypeIsUnsupported_ThrowsNotSupportedException()
    {
        var principal = CreatePrincipal(TestUserId.ToString(), TestOrganizationId.ToString());

        Assert.Throws<NotSupportedException>(() => UserContextHelper.GetUserContext<string>(principal));
    }
}
