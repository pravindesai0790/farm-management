using FarmManagement.API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace FarmManagement.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
