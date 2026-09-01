using FarmManagement.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FarmManagement.API.Authorization;

public sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string PolicyPrefix = AuthorizationConstants.PermissionPolicyPrefix;

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal) ||
            policyName.Length == PolicyPrefix.Length)
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[PolicyPrefix.Length..].Trim();
        if (permission.Length == 0)
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
