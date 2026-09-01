using Microsoft.AspNetCore.Authorization;

namespace FarmManagement.API.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission.Trim();
    }

    public string Permission { get; }
}
