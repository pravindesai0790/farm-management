using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models.Authentication;
using FarmManagement.Application.Interfaces.Authentication;
using FarmManagement.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FarmManagement.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public AccessTokenResult GenerateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(permissions);

        var jwtOptions = options.Value;
        jwtOptions.Validate();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(
                AuthorizationConstants.OrganizationIdClaimType,
                user.OrganizationId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(ToClaims(AuthorizationConstants.RoleClaimType, roles));
        claims.AddRange(ToClaims(AuthorizationConstants.PermissionClaimType, permissions));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            expiresAt,
            checked((int)(expiresAt - now).TotalSeconds));
    }

    private static IEnumerable<Claim> ToClaims(
        string claimType,
        IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new Claim(claimType, value.Trim()))
            .DistinctBy(claim => claim.Value, StringComparer.Ordinal);
    }
}
