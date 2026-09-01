using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Authentication;

public interface IPasswordService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string passwordHash, string password);
}
