using FarmManagement.Application.DTOs;

namespace FarmManagement.Application.Interfaces;

public interface ISystemService
{
    SystemPingResponse GetPing();
}
