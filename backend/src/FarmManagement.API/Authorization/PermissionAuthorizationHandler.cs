using FarmManagement.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;

namespace FarmManagement.API.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(
                AuthorizationConstants.PermissionClaimType,
                requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
