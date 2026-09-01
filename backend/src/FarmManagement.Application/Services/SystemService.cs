using FarmManagement.Application.DTOs;
using FarmManagement.Application.Interfaces;

namespace FarmManagement.Application.Services;

public sealed class SystemService : ISystemService
{
    public SystemPingResponse GetPing()
    {
        return new SystemPingResponse(
            "Farm Management API is running",
            DateTimeOffset.UtcNow);
    }
}
