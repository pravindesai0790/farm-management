using FarmManagement.API.Configuration;
using FarmManagement.API.Helpers;
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
    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// Web clients receive an HttpOnly cookie containing the refresh token.
    /// Mobile clients (signaled by X-Client-Platform: Mobile) receive the refresh token directly in the response payload
    /// for storage in the device's hardware-backed SecureStore (iOS Keychain / Android Keystore).
    /// </summary>
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

        var isMobile = IsMobileClient();

        if (!isMobile)
        {
            SetRefreshTokenCookie(result);
        }

        return Ok(new LoginResponse(
            result.AccessToken,
            result.ExpiresIn,
            result.User!,
            isMobile ? result.RefreshToken : null));
    }

    /// <summary>
    /// Refreshes an expired JWT access token using a valid refresh token.
    /// Supports both web clients (extracting the token from HttpOnly cookies)
    /// and mobile clients (extracting the token from the request body).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MobileRefreshRequest? mobileRequest,
        CancellationToken cancellationToken)
    {
        var isMobile = IsMobileClient();
        var token = isMobile && !string.IsNullOrWhiteSpace(mobileRequest?.RefreshToken)
            ? mobileRequest.RefreshToken
            : Request.Cookies[cookieOptions.Value.Name] ?? string.Empty;

        var result = await authenticationService.RefreshAsync(
            token,
            GetIpAddress(),
            cancellationToken);

        if (!isMobile)
        {
            SetRefreshTokenCookie(result);
        }

        return Ok(new RefreshResponse(
            result.AccessToken,
            result.ExpiresIn,
            isMobile ? result.RefreshToken : null));
    }

    /// <summary>
    /// Revokes the current session's refresh token and removes credentials.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MobileRefreshRequest? mobileRequest,
        CancellationToken cancellationToken)
    {
        var isMobile = IsMobileClient();
        var token = isMobile && !string.IsNullOrWhiteSpace(mobileRequest?.RefreshToken)
            ? mobileRequest.RefreshToken
            : Request.Cookies[cookieOptions.Value.Name];

        await authenticationService.LogoutAsync(
            token,
            GetIpAddress(),
            cancellationToken);

        if (!isMobile)
        {
            DeleteRefreshTokenCookie();
        }

        return NoContent();
    }

    /// <summary>
    /// Inspects request headers to determine if the caller is the native mobile application.
    /// </summary>
    private bool IsMobileClient() =>
        Request.Headers.TryGetValue("X-Client-Platform", out var platform)
        && platform.ToString().Equals("Mobile", StringComparison.OrdinalIgnoreCase);

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

    private Guid GetAuthenticatedUserId() => UserContextHelper.GetAuthenticatedUserId(User);

    private static SameSiteMode ParseSameSite(string? value) =>
        Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var sameSite)
            ? sameSite
            : SameSiteMode.Lax;
}
