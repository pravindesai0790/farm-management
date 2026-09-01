namespace FarmManagement.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "FarmManagement";

    public string Audience { get; set; } = "FarmManagement.Web";

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must contain at least 32 characters.");
        }

        if (AccessTokenMinutes != 15)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes must be 15 minutes.");
        }

        if (RefreshTokenDays != 7)
        {
            throw new InvalidOperationException(
                "Jwt:RefreshTokenDays must be 7 days.");
        }
    }
}
