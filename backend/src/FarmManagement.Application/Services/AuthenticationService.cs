using System.Text.Json;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models.Authentication;
using FarmManagement.Application.DTOs.Authentication;
using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;

namespace FarmManagement.Application.Services;

public sealed class AuthenticationService(
    IAuthenticationStore authenticationStore,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : IAuthenticationService
{
    private const int MaximumFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly HashSet<string> CommonPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "123456789012",
            "1234567890!aA",
            "admin123!Aaa",
            "letmein123!A",
            "qwerty123!A",
            "welcome123!A"
        };

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var email = request.Email!.Trim().ToLowerInvariant();
        var user = await authenticationStore.FindUserByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        var now = DateTimeOffset.UtcNow;
        if (!user.IsActive || !user.Organization.IsActive)
        {
            throw new UnauthorizedAccessException("This account is not active.");
        }

        if (user.IsLockedOut(now))
        {
            throw new UnauthorizedAccessException("This account is temporarily locked.");
        }

        if (!passwordService.VerifyPassword(user, user.PasswordHash, request.Password!))
        {
            user.RecordFailedLogin(now, MaximumFailedLoginAttempts, LockoutDuration);
            AddAudit(user, "User.LoginFailed", ipAddress);
            await authenticationStore.SaveChangesAsync(cancellationToken);

            throw user.IsLockedOut(now)
                ? new UnauthorizedAccessException("This account is temporarily locked.")
                : InvalidCredentials();
        }

        user.RecordSuccessfulLogin(now);
        var rolesAndPermissions = GetRolesAndPermissions(user);
        var accessToken = jwtTokenService.GenerateAccessToken(
            user,
            rolesAndPermissions.Roles,
            rolesAndPermissions.Permissions);
        var refreshToken = await refreshTokenService.CreateAsync(
            user.Id,
            ClientType.Web,
            createdByIp: ipAddress,
            cancellationToken: cancellationToken);

        AddAudit(user, "User.Login", ipAddress);
        await authenticationStore.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            ToUserResponse(user, rolesAndPermissions),
            refreshToken.Token,
            refreshToken.ExpiresAt);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rotation = await refreshTokenService.RotateAsync(
            refreshToken,
            ClientType.Web,
            ipAddress,
            cancellationToken: cancellationToken);

        if (rotation is null)
        {
            throw new UnauthorizedAccessException("The refresh token is invalid or expired.");
        }

        var user = await authenticationStore.FindUserByIdAsync(
            rotation.UserId,
            cancellationToken);
        if (user is null || !user.IsActive || !user.Organization.IsActive)
        {
            throw new UnauthorizedAccessException("The account is not available.");
        }

        var rolesAndPermissions = GetRolesAndPermissions(user);
        var accessToken = jwtTokenService.GenerateAccessToken(
            user,
            rolesAndPermissions.Roles,
            rolesAndPermissions.Permissions);

        AddAudit(user, "User.RefreshTokenRotated", ipAddress);
        await authenticationStore.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            null,
            rotation.Token,
            rotation.ExpiresAt);
    }

    public async Task LogoutAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var token = await refreshTokenService.ValidateAsync(
            refreshToken,
            ClientType.Web,
            cancellationToken);
        if (token is null)
        {
            return;
        }

        var user = await authenticationStore.FindUserByIdAsync(token.UserId, cancellationToken);
        var revoked = await refreshTokenService.RevokeAsync(
            refreshToken,
            ClientType.Web,
            ipAddress,
            cancellationToken);

        if (revoked && user is not null)
        {
            AddAudit(user, "User.Logout", ipAddress);
            await authenticationStore.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AuthenticationUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await authenticationStore.FindUserByIdAsync(userId, cancellationToken);
        return user is null || !user.IsActive || !user.Organization.IsActive
            ? null
            : ToUserResponse(user, GetRolesAndPermissions(user));
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateChangePasswordRequest(request);

        var user = await authenticationStore.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || !user.Organization.IsActive)
        {
            throw new UnauthorizedAccessException("The account is not available.");
        }

        if (!passwordService.VerifyPassword(user, user.PasswordHash, request.CurrentPassword!))
        {
            throw new UnauthorizedAccessException("The current password is incorrect.");
        }

        ValidateNewPassword(user, request.NewPassword!);

        var now = DateTimeOffset.UtcNow;
        user.ChangePassword(passwordService.HashPassword(user, request.NewPassword!), now);
        await refreshTokenService.RevokeAllForUserAsync(user.Id, ipAddress, cancellationToken);

        AddAudit(user, "User.PasswordChanged", ipAddress);
        await authenticationStore.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateLoginRequest(LoginRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException(
                "Validation failed",
                new Dictionary<string, string[]> { ["email"] = ["Email is required."] });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException(
                "Validation failed",
                new Dictionary<string, string[]> { ["password"] = ["Password is required."] });
        }
    }

    private static void ValidateChangePasswordRequest(ChangePasswordRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (request is null || string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is required."];
        }

        if (request is null || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors["newPassword"] = ["New password is required."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Validation failed", errors);
        }
    }

    private static void ValidateNewPassword(User user, string password)
    {
        var errors = new List<string>();
        if (password.Length < 12)
        {
            errors.Add("Password must be at least 12 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (string.Equals(password, user.Email, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(password, user.FirstName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(password, user.LastName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Password must not match the email address or name.");
        }

        if (CommonPasswords.Contains(password))
        {
            errors.Add("Password is too common.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(
                "Password does not meet the password policy.",
                new Dictionary<string, string[]> { ["newPassword"] = [.. errors] });
        }
    }

    private static (IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)
        GetRolesAndPermissions(User user)
    {
        var roles = user.UserRoles
            .Where(userRole => userRole.Role.IsActive)
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var permissions = user.UserRoles
            .Where(userRole => userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Select(rolePermission => rolePermission.Permission.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();

        return (roles, permissions);
    }

    private static AuthenticationUserResponse ToUserResponse(
        User user,
        (IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions) rolesAndPermissions) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.OrganizationId,
            rolesAndPermissions.Roles,
            rolesAndPermissions.Permissions);

    private void AddAudit(User user, string action, string? ipAddress)
    {
        authenticationStore.AddAuditLog(new AuditLog(
            action,
            user.OrganizationId,
            user.Id,
            entityType: "User",
            entityId: user.Id,
            details: (JsonDocument?)null,
            ipAddress: ipAddress));
    }

    private static UnauthorizedAccessException InvalidCredentials() =>
        new("Invalid email or password.");
}
