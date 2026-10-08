using FarmManagement.API.Helpers;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/sprays")]
[Authorize]
public sealed class SpraysController(ISprayService sprayService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Spray.View")]
    public async Task<IActionResult> List([FromQuery] SprayListQuery query, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<SprayActor>(User);
        var result = await sprayService.ListAsync(actor, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Spray.View")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<SprayActor>(User);
        var result = await sprayService.GetAsync(actor, id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:Spray.Create")]
    public async Task<IActionResult> Create([FromBody] CreateSprayDraftRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<SprayActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sprayService.CreateDraftAsync(actor, request, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Spray.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSprayDraftRequest request, CancellationToken cancellationToken = default)
    {
        var actor = UserContextHelper.GetUserContext<SprayActor>(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sprayService.UpdateDraftAsync(actor, id, request, ipAddress, cancellationToken);
        return Ok(result);
    }
}
