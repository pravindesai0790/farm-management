namespace FarmManagement.Application.Common.Constants;

public static class AuthorizationConstants
{
    public const string OrganizationIdClaimType = "organization_id";

    public const string PermissionClaimType = "permissions";

    public const string RoleClaimType = "roles";

    public const string PermissionPolicyPrefix = "Permission:";

    public static string PermissionPolicyName(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return $"{PermissionPolicyPrefix}{permission.Trim()}";
    }
}
