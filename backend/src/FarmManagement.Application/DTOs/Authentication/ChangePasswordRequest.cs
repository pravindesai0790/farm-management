namespace FarmManagement.Application.DTOs.Authentication;

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
