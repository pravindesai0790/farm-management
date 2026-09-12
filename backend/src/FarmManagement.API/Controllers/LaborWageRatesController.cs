using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/wage-rates")]
[Authorize]
public sealed class LaborWageRatesController(ILaborWageRateService wageRateService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:WorkerWage.View")]
    public async Task<ActionResult<PagedResponse<LaborWageRateResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? gender = null,
        [FromQuery] string? wageType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateOnly? businessDate = null,
        CancellationToken cancellationToken = default) =>
        Ok(await wageRateService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            gender,
            wageType,
            isActive,
            businessDate,
            cancellationToken));

    [HttpGet("all")]
    [Authorize(Policy = "Permission:WorkerWage.View")]
    public async Task<ActionResult<IReadOnlyList<LaborWageRateResponse>>> ListAll(
        [FromQuery] string? gender = null,
        [FromQuery] string? wageType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await wageRateService.ListAllAsync(
            GetUserContext(),
            gender,
            wageType,
            isActive,
            cancellationToken));

    [HttpGet("applicable")]
    [Authorize(Policy = "Permission:WorkerWage.View")]
    public async Task<ActionResult<LaborWageRateResponse>> GetApplicable(
        [FromQuery] string gender,
        [FromQuery] string wageType,
        [FromQuery] DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        var parsedGender = LaborWageRateService.ParseGender(gender);
        var parsedWageType = LaborWageRateService.ParseWageType(wageType);

        var rate = await wageRateService.GetApplicableRateAsync(
            GetUserContext(),
            parsedGender,
            parsedWageType,
            businessDate,
            cancellationToken);

        if (rate is null)
        {
            return NotFound(new { message = $"No applicable wage rate found for {gender} and {wageType} on {businessDate:yyyy-MM-dd}." });
        }

        return Ok(rate);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:WorkerWage.View")]
    public async Task<ActionResult<LaborWageRateResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await wageRateService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:WorkerWage.Create")]
    public async Task<ActionResult<LaborWageRateResponse>> Create(
        [FromBody] CreateLaborWageRateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await wageRateService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:WorkerWage.Update")]
    public async Task<ActionResult<LaborWageRateResponse>> Update(
        Guid id,
        [FromBody] UpdateLaborWageRateRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wageRateService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:WorkerWage.Update")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        await wageRateService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:WorkerWage.Update")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        await wageRateService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private LaborWageRateActor GetUserContext() => UserContextHelper.GetUserContext<LaborWageRateActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
