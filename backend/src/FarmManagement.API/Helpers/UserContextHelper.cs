using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Helpers;

/// <summary>
/// Provides decoupled helper methods for extracting authenticated user identity,
/// organization tenant scope, and constructing user context/actor models for controllers.
/// </summary>
public static class UserContextHelper
{
    public static (Guid UserId, Guid OrganizationId) GetUserIds(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = principal.FindFirstValue(AuthorizationConstants.OrganizationIdClaimType);

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId))
        {
            throw new UnauthorizedAccessException("The access token is invalid.");
        }

        return (userId, organizationId);
    }

    public static Guid GetAuthenticatedUserId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("The access token is invalid.");
    }

    public static bool CanManageAllOrganizations(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Claims.Any(claim =>
            claim.Type == AuthorizationConstants.OrganizationScopeClaimType &&
            string.Equals(
                claim.Value,
                AuthorizationConstants.AllOrganizationsScope,
                StringComparison.Ordinal));
    }

    public static bool IsSuperAdmin(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Claims.Any(claim =>
            claim.Type == AuthorizationConstants.RoleClaimType &&
            string.Equals(
                claim.Value,
                AuthorizationConstants.SuperAdminRoleName,
                StringComparison.Ordinal));
    }

    public static TActor GetUserContext<TActor>(this ControllerBase controller) =>
        GetUserContext<TActor>(controller.User);

    public static TActor GetUserContext<TActor>(this ClaimsPrincipal principal)
    {
        var (userId, organizationId) = GetUserIds(principal);

        if (typeof(TActor) == typeof(FarmActor))
        {
            return (TActor)(object)new FarmActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(CropCycleActor))
        {
            return (TActor)(object)new CropCycleActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(RoleAdministrationActor))
        {
            return (TActor)(object)new RoleAdministrationActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(LaborActivityActor))
        {
            return (TActor)(object)new LaborActivityActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(WorkerActor))
        {
            return (TActor)(object)new WorkerActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(ContractorActor))
        {
            return (TActor)(object)new ContractorActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(LaborCategoryActor))
        {
            return (TActor)(object)new LaborCategoryActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(LaborWageRateActor))
        {
            return (TActor)(object)new LaborWageRateActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(EarningsActor))
        {
            return (TActor)(object)new EarningsActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(PaymentActor))
        {
            return (TActor)(object)new PaymentActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(SettlementActor))
        {
            return (TActor)(object)new SettlementActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(DashboardActor))
        {
            return (TActor)(object)new DashboardActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(PlantationActor))
        {
            return (TActor)(object)new PlantationActor(userId, organizationId);
        }

        if (typeof(TActor) == typeof(UserAdministrationActor))
        {
            return (TActor)(object)new UserAdministrationActor(
                userId,
                organizationId,
                CanManageAllOrganizations(principal));
        }

        if (typeof(TActor) == typeof(OrganizationActor))
        {
            return (TActor)(object)new OrganizationActor(
                userId,
                organizationId,
                CanManageAllOrganizations(principal));
        }

        if (typeof(TActor) == typeof(MasterDataActor))
        {
            return (TActor)(object)new MasterDataActor(
                userId,
                organizationId,
                CanManageAllOrganizations(principal));
        }

        if (typeof(TActor) == typeof(CropActor))
        {
            return (TActor)(object)new CropActor(
                userId,
                organizationId,
                IsSuperAdmin(principal));
        }

        if (typeof(TActor) == typeof(CropLifecycleTemplateActor))
        {
            return (TActor)(object)new CropLifecycleTemplateActor(
                userId,
                organizationId,
                IsSuperAdmin(principal));
        }

        throw new NotSupportedException($"User context type '{typeof(TActor).Name}' is not supported.");
    }

    public static TActor GetUserContext<TActor>(ClaimsPrincipal principal, Func<Guid, Guid, TActor> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var (userId, organizationId) = GetUserIds(principal);
        return factory(userId, organizationId);
    }

    public static TActor GetUserContext<TActor>(
        ClaimsPrincipal principal,
        Func<Guid, Guid, bool, TActor> factory,
        bool useScopeFlag = true)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var (userId, organizationId) = GetUserIds(principal);
        var flag = useScopeFlag ? CanManageAllOrganizations(principal) : IsSuperAdmin(principal);
        return factory(userId, organizationId, flag);
    }

    public static TActor GetActor<TActor>(this ClaimsPrincipal principal) => GetUserContext<TActor>(principal);

    public static TActor GetActor<TActor>(this ControllerBase controller) => GetUserContext<TActor>(controller);
}
