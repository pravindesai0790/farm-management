using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FarmManagement.API.Configuration;
using FarmManagement.Application.DTOs.Authentication;
using FarmManagement.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService,
    IOptions<RefreshTokenCookieOptions> cookieOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            request,
            GetIpAddress(),
            cancellationToken);

        SetRefreshTokenCookie(result);
        return Ok(new LoginResponse(result.AccessToken, result.ExpiresIn, result.User!));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(
            Request.Cookies[cookieOptions.Value.Name] ?? string.Empty,
            GetIpAddress(),
            cancellationToken);

        SetRefreshTokenCookie(result);
        return Ok(new RefreshResponse(result.AccessToken, result.ExpiresIn));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authenticationService.LogoutAsync(
            Request.Cookies[cookieOptions.Value.Name],
            GetIpAddress(),
            cancellationToken);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticationUserResponse>> Me(
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await authenticationService.GetCurrentUserAsync(userId, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authenticationService.ChangePasswordAsync(
            GetAuthenticatedUserId(),
            request,
            GetIpAddress(),
            cancellationToken);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(AuthenticationResult result)
    {
        var options = BuildCookieOptions();
        options.Expires = result.RefreshTokenExpiresAt;
        options.MaxAge = result.RefreshTokenExpiresAt - DateTimeOffset.UtcNow;
        Response.Cookies.Append(
            cookieOptions.Value.Name,
            result.RefreshToken,
            options);
    }

    private void DeleteRefreshTokenCookie()
    {
        var options = BuildCookieOptions();
        Response.Cookies.Delete(cookieOptions.Value.Name, options);
    }

    private CookieOptions BuildCookieOptions()
    {
        var settings = cookieOptions.Value;
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = settings.Secure,
            SameSite = ParseSameSite(settings.SameSite),
            Path = settings.Path,
            IsEssential = true
        };
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("The access token is invalid.");
    }

    private static SameSiteMode ParseSameSite(string? value) =>
        Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var sameSite)
            ? sameSite
            : SameSiteMode.Lax;
}
