using FarmManagement.API.Configuration;
using FarmManagement.API.Controllers;
using FarmManagement.Application.DTOs.Authentication;
using FarmManagement.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

/// <summary>
/// Unit and integration tests verifying the dual-mode authentication behavior
/// for Web (HttpOnly cookies) and Mobile (Bearer payload tokens via X-Client-Platform header).
/// </summary>
public sealed class MobileAuthenticationTests
{
    private readonly FakeAuthenticationService _authService = new();
    private readonly IOptions<RefreshTokenCookieOptions> _cookieOptions = Options.Create(new RefreshTokenCookieOptions
    {
        Name = "fm_refresh_token",
        Secure = true,
        SameSite = "Strict"
    });

    private AuthController CreateController(string? clientPlatformHeader = null)
    {
        var controller = new AuthController(_authService, _cookieOptions);
        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(clientPlatformHeader))
        {
            httpContext.Request.Headers["X-Client-Platform"] = clientPlatformHeader;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task Login_WhenWebClient_SetsCookieAndReturnsNullRefreshTokenInPayload()
    {
        // Arrange: Standard web login request without mobile platform header
        var controller = CreateController(clientPlatformHeader: null);
        var loginRequest = new LoginRequest("admin@farm.local", "P@ssword123!");
        var expectedUser = new AuthenticationUserResponse(Guid.NewGuid(), "Admin", "User", "admin@farm.local", Guid.NewGuid(), "Farm Alpha", ["SuperAdmin"], ["Users.View"]);
        _authService.LoginResult = new AuthenticationResult("access_token_123", 900, expectedUser, "refresh_token_abc", DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var actionResult = await controller.Login(loginRequest, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);

        Assert.Equal("access_token_123", response.AccessToken);
        Assert.Equal(900, response.ExpiresIn);
        Assert.Null(response.RefreshToken); // Web clients must NOT receive refresh token in JSON

        // Verify cookie is set on the response
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains("fm_refresh_token=refresh_token_abc", setCookieHeader);
        Assert.Contains("httponly", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WhenMobileClient_ReturnsRefreshTokenInPayloadAndDoesNotSetCookie()
    {
        // Arrange: Mobile client identified via X-Client-Platform: Mobile
        var controller = CreateController(clientPlatformHeader: "Mobile");
        var loginRequest = new LoginRequest("supervisor@farm.local", "P@ssword123!");
        var expectedUser = new AuthenticationUserResponse(Guid.NewGuid(), "Field", "Supervisor", "supervisor@farm.local", Guid.NewGuid(), "Farm Alpha", ["Supervisor"], ["Attendance.Create"]);
        _authService.LoginResult = new AuthenticationResult("access_token_456", 900, expectedUser, "refresh_token_mobile_xyz", DateTimeOffset.UtcNow.AddDays(30));

        // Act
        var actionResult = await controller.Login(loginRequest, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);

        Assert.Equal("access_token_456", response.AccessToken);
        Assert.Equal("refresh_token_mobile_xyz", response.RefreshToken); // Mobile client receives token in payload for SecureStore

        // Verify cookie is NOT set for mobile clients
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        Assert.DoesNotContain("fm_refresh_token", setCookieHeader);
    }

    [Fact]
    public async Task Refresh_WhenWebClient_ReadsFromCookieAndSetsRotatedCookie()
    {
        // Arrange: Web client with refresh token cookie
        var controller = CreateController(clientPlatformHeader: null);
        controller.Request.Headers.Cookie = "fm_refresh_token=cookie_token_123";

        _authService.RefreshResult = new AuthenticationResult("new_access_token", 900, null, "new_refresh_token_cookie", DateTimeOffset.UtcNow.AddDays(7));

        // Act
        var actionResult = await controller.Refresh(null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<RefreshResponse>(okResult.Value);

        Assert.Equal("new_access_token", response.AccessToken);
        Assert.Null(response.RefreshToken);
        Assert.Equal("cookie_token_123", _authService.LastRefreshTokenProvided);

        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains("fm_refresh_token=new_refresh_token_cookie", setCookieHeader);
    }

    [Fact]
    public async Task Refresh_WhenMobileClient_ReadsFromBodyAndReturnsRotatedTokenInPayload()
    {
        // Arrange: Mobile client passing token in body
        var controller = CreateController(clientPlatformHeader: "Mobile");
        var mobileRequest = new MobileRefreshRequest("mobile_refresh_token_input");

        _authService.RefreshResult = new AuthenticationResult("mobile_new_access_token", 900, null, "mobile_rotated_refresh_token", DateTimeOffset.UtcNow.AddDays(30));

        // Act
        var actionResult = await controller.Refresh(mobileRequest, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<RefreshResponse>(okResult.Value);

        Assert.Equal("mobile_new_access_token", response.AccessToken);
        Assert.Equal("mobile_rotated_refresh_token", response.RefreshToken); // Rotated token returned for SecureStore update
        Assert.Equal("mobile_refresh_token_input", _authService.LastRefreshTokenProvided);

        // Ensure no cookie written
        var setCookieHeader = controller.Response.Headers.SetCookie.ToString();
        Assert.DoesNotContain("fm_refresh_token", setCookieHeader);
    }

    [Fact]
    public async Task Logout_WhenMobileClient_RevokesTokenFromBody()
    {
        // Arrange: Mobile logout with token in body
        var controller = CreateController(clientPlatformHeader: "Mobile");
        var mobileRequest = new MobileRefreshRequest("token_to_revoke");

        // Act
        var result = await controller.Logout(mobileRequest, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal("token_to_revoke", _authService.LastRevokedToken);
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public AuthenticationResult? LoginResult { get; set; }
        public AuthenticationResult? RefreshResult { get; set; }
        public string? LastRefreshTokenProvided { get; private set; }
        public string? LastRevokedToken { get; private set; }

        public Task<AuthenticationResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoginResult ?? new AuthenticationResult("default_acc", 3600, null, "default_ref", DateTimeOffset.UtcNow.AddDays(7)));
        }

        public Task<AuthenticationResult> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
        {
            LastRefreshTokenProvided = refreshToken;
            return Task.FromResult(RefreshResult ?? new AuthenticationResult("default_acc", 3600, null, "default_ref", DateTimeOffset.UtcNow.AddDays(7)));
        }

        public Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
        {
            LastRevokedToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<AuthenticationUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticationUserResponse?>(new AuthenticationUserResponse(userId, "Test", "User", "test@farm.local", Guid.NewGuid(), "Farm Alpha", ["Admin"], []));
        }

        public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
