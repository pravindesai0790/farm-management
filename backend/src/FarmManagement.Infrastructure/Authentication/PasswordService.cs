using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FarmManagement.Infrastructure.Authentication;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string passwordHash, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        var result = passwordHasher.VerifyHashedPassword(user, passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
