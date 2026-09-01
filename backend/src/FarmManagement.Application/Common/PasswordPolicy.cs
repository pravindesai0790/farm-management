using FarmManagement.Application.Common.Exceptions;

namespace FarmManagement.Application.Common;

public static class PasswordPolicy
{
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

    public static void ValidateNewPassword(
        string? password,
        string email,
        string firstName,
        string lastName,
        string fieldName = "newPassword")
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
        }
        else
        {
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

            if (string.Equals(password, email, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(password, firstName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(password, lastName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Password must not match the email address or name.");
            }

            if (CommonPasswords.Contains(password))
            {
                errors.Add("Password is too common.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(
                "Password does not meet the password policy.",
                new Dictionary<string, string[]> { [fieldName] = [.. errors] });
        }
    }
}
