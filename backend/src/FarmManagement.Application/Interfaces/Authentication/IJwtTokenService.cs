using FarmManagement.Application.Common.Models.Authentication;
using FarmManagement.Domain.Entities;

namespace FarmManagement.Application.Interfaces.Authentication;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);
}
