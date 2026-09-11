using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/contractors")]
[Authorize]
public sealed class ContractorsController(IContractorService contractorService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Contractor.View")]
    public async Task<ActionResult<PagedResponse<ContractorResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await contractorService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Contractor.View")]
    public async Task<ActionResult<ContractorResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await contractorService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:Contractor.Create")]
    public async Task<ActionResult<ContractorResponse>> Create(
        [FromBody] CreateContractorRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await contractorService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Contractor.Update")]
    public async Task<ActionResult<ContractorResponse>> Update(
        Guid id,
        [FromBody] UpdateContractorRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await contractorService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Contractor.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        await contractorService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Contractor.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        await contractorService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    private ContractorActor GetUserContext() => UserContextHelper.GetUserContext<ContractorActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
