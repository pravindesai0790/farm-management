namespace FarmManagement.API.Configuration;

public sealed class RefreshTokenCookieOptions
{
    public const string SectionName = "Authentication:RefreshTokenCookie";

    public string Name { get; set; } = "farm_refresh_token";

    public bool HttpOnly { get; set; } = true;

    public bool Secure { get; set; } = true;

    public string SameSite { get; set; } = "Lax";

    public string Path { get; set; } = "/api/auth";
}
