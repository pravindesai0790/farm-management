using FarmManagement.Application.DTOs;
using FarmManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(ISystemService systemService) : ControllerBase
{
    [HttpGet("ping")]
    [ProducesResponseType(typeof(SystemPingResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemPingResponse> Ping()
    {
        return Ok(systemService.GetPing());
    }
}
