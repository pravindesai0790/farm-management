using FarmManagement.API.Helpers;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManagement.API.Controllers;

[ApiController]
[Route("api/labor/workers")]
[Authorize]
public sealed class WorkersController(
    IWorkerService workerService,
    IWorkerFarmAssignmentService farmAssignmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Permission:Worker.View")]
    public async Task<ActionResult<PagedResponse<WorkerResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? contractorId = null,
        [FromQuery] Guid? laborCategoryId = null,
        [FromQuery] string? gender = null,
        [FromQuery] string? employmentType = null,
        CancellationToken cancellationToken = default) =>
        Ok(await workerService.ListAsync(
            GetUserContext(),
            page,
            pageSize,
            search,
            isActive,
            contractorId,
            laborCategoryId,
            gender,
            employmentType,
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:Worker.View")]
    public async Task<ActionResult<WorkerDetailResponse>> Get(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await workerService.GetAsync(GetUserContext(), id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Permission:Worker.Create")]
    public async Task<ActionResult<WorkerDetailResponse>> Create(
        [FromBody] CreateWorkerRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await workerService.CreateAsync(GetUserContext(), request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<ActionResult<WorkerDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateWorkerRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await workerService.UpdateAsync(GetUserContext(), id, request, GetIpAddress(), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "Permission:Worker.Activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        await workerService.ActivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "Permission:Worker.Deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        await workerService.DeactivateAsync(GetUserContext(), id, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpGet("{workerId:guid}/farms")]
    [Authorize(Policy = "Permission:Worker.View")]
    public async Task<ActionResult<IReadOnlyList<WorkerFarmAssignmentResponse>>> ListFarmAssignments(
        Guid workerId,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await farmAssignmentService.ListByWorkerAsync(GetUserContext(), workerId, isActive, cancellationToken));

    [HttpGet("{workerId:guid}/farms/{assignmentId:guid}")]
    [Authorize(Policy = "Permission:Worker.View")]
    public async Task<ActionResult<WorkerFarmAssignmentResponse>> GetFarmAssignment(
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        Ok(await farmAssignmentService.GetAsync(GetUserContext(), workerId, assignmentId, cancellationToken));

    [HttpPost("{workerId:guid}/farms")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<ActionResult<WorkerFarmAssignmentResponse>> CreateFarmAssignment(
        Guid workerId,
        [FromBody] CreateWorkerFarmAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await farmAssignmentService.CreateAsync(GetUserContext(), workerId, request, GetIpAddress(), cancellationToken);
        return CreatedAtAction(nameof(GetFarmAssignment), new { workerId, assignmentId = result.Id }, result);
    }

    [HttpPut("{workerId:guid}/farms/{assignmentId:guid}")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<ActionResult<WorkerFarmAssignmentResponse>> UpdateFarmAssignment(
        Guid workerId,
        Guid assignmentId,
        [FromBody] UpdateWorkerFarmAssignmentRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await farmAssignmentService.UpdateAsync(GetUserContext(), workerId, assignmentId, request, GetIpAddress(), cancellationToken));

    [HttpPost("{workerId:guid}/farms/{assignmentId:guid}/deactivate")]
    [HttpPatch("{workerId:guid}/farms/{assignmentId:guid}/deactivate")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<IActionResult> DeactivateFarmAssignment(
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        await farmAssignmentService.DeactivateAsync(GetUserContext(), workerId, assignmentId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{workerId:guid}/farms/{assignmentId:guid}/activate")]
    [HttpPatch("{workerId:guid}/farms/{assignmentId:guid}/activate")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<IActionResult> ActivateFarmAssignment(
        Guid workerId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        await farmAssignmentService.ActivateAsync(GetUserContext(), workerId, assignmentId, GetIpAddress(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{workerId:guid}/farms/{assignmentId:guid}/end")]
    [Authorize(Policy = "Permission:Worker.Update")]
    public async Task<ActionResult<WorkerFarmAssignmentResponse>> EndFarmAssignment(
        Guid workerId,
        Guid assignmentId,
        [FromBody] EndWorkerFarmAssignmentRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await farmAssignmentService.EndAssignmentAsync(GetUserContext(), workerId, assignmentId, request, GetIpAddress(), cancellationToken));

    [HttpGet("/api/labor/farms/{farmId:guid}/workers")]
    [HttpGet("/api/farms/{farmId:guid}/workers")]
    [Authorize(Policy = "Permission:Worker.View")]
    public async Task<ActionResult<IReadOnlyList<WorkerFarmAssignmentResponse>>> ListFarmWorkers(
        Guid farmId,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await farmAssignmentService.ListByFarmAsync(GetUserContext(), farmId, isActive, cancellationToken));

    private WorkerActor GetUserContext() => UserContextHelper.GetUserContext<WorkerActor>(User);

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
