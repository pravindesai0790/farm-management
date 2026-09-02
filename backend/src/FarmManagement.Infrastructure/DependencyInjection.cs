using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Application.Interfaces.Farms;
using FarmManagement.Application.Interfaces.Users;
using FarmManagement.Application.Interfaces.Roles;
using FarmManagement.Infrastructure.Authentication;
using FarmManagement.Infrastructure.Persistence;
using FarmManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FarmManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<IAuthenticationStore, AuthenticationStore>();
        services.AddScoped<IUserAdministrationStore, UserAdministrationStore>();
        services.AddScoped<IRoleAdministrationStore, RoleAdministrationStore>();
        services.AddScoped<IFarmStore, FarmStore>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IdentityDataSeeder>();

        return services;
    }
}
