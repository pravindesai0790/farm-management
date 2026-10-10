using System.Security.Claims;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Irrigation;
using FarmManagement.Application.Interfaces.Irrigation;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

public class IrrigationsControllerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private IrrigationsController CreateController(IIrrigationService service)
    {
        var controller = new IrrigationsController(service);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new(AuthorizationConstants.OrganizationIdClaimType, _orgId.ToString()),
            new("permissions", "Irrigation.View"),
            new("permissions", "Irrigation.Create"),
            new("permissions", "Irrigation.Update"),
            new("permissions", "Irrigation.Schedule"),
            new("permissions", "Irrigation.Start"),
            new("permissions", "Irrigation.Complete"),
            new("permissions", "Irrigation.Cancel"),
            new("permissions", "IrrigationMethod.View")
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task List_ReturnsOkWithPagedResult()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var query = new IrrigationListQuery(FarmId: Guid.NewGuid());

        var actionResult = await controller.List(query);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<PagedResponse<IrrigationListItemResponse>>(okResult.Value);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Summary_ReturnsOkWithSummaryCounts()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.Summary(null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationSummaryCountsResponse>(okResult.Value);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task ListMethods_ReturnsOkWithMethods()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);

        var actionResult = await controller.ListMethods(true);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsAssignableFrom<IReadOnlyList<IrrigationMethodDto>>(okResult.Value);
        Assert.Single(result);
    }

    [Fact]
    public async Task Get_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();

        var actionResult = await controller.Get(eventId);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtActionWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var request = new CreateIrrigationDraftRequest(FarmId: Guid.NewGuid(), FarmAreaId: Guid.NewGuid());

        var actionResult = await controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(IrrigationsController.Get), createdResult.ActionName);
        var result = Assert.IsType<IrrigationDetailsResponse>(createdResult.Value);
        Assert.Equal(request.FarmId, result.FarmId);
    }

    [Fact]
    public async Task Update_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new UpdateIrrigationDraftRequest(FarmId: Guid.NewGuid(), FarmAreaId: Guid.NewGuid());

        var actionResult = await controller.Update(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task Schedule_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new ScheduleIrrigationRequest(DateTimeOffset.UtcNow);

        var actionResult = await controller.Schedule(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task Reschedule_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new RescheduleIrrigationRequest(DateTimeOffset.UtcNow);

        var actionResult = await controller.Reschedule(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task Start_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new StartIrrigationRequest(DateTimeOffset.UtcNow);

        var actionResult = await controller.Start(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task Complete_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new CompleteIrrigationRequest(Guid.NewGuid());

        var actionResult = await controller.Complete(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task RecordCompleted_ReturnsCreatedAtActionWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var request = new RecordCompletedIrrigationRequest(
            FarmId: Guid.NewGuid(),
            FarmAreaId: Guid.NewGuid(),
            IrrigationMethodId: Guid.NewGuid());

        var actionResult = await controller.RecordCompleted(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(nameof(IrrigationsController.Get), createdResult.ActionName);
        var result = Assert.IsType<IrrigationDetailsResponse>(createdResult.Value);
        Assert.Equal(request.FarmId, result.FarmId);
    }

    [Fact]
    public async Task Cancel_ReturnsOkWithDetails()
    {
        var fakeService = new FakeIrrigationService();
        var controller = CreateController(fakeService);
        var eventId = Guid.NewGuid();
        var request = new CancelIrrigationRequest("Water shortage");

        var actionResult = await controller.Cancel(eventId, request);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<IrrigationDetailsResponse>(okResult.Value);
        Assert.Equal(eventId, result.Id);
    }

    private sealed class FakeIrrigationService : IIrrigationService
    {
        public Task<PagedResponse<IrrigationListItemResponse>> ListAsync(IrrigationActor actor, IrrigationListQuery query, CancellationToken cancellationToken = default)
        {
            var item = new IrrigationListItemResponse(
                Id: Guid.NewGuid(),
                FarmId: query.FarmId ?? Guid.NewGuid(),
                FarmName: "Test Farm",
                FarmAreaId: Guid.NewGuid(),
                FarmAreaName: "Area 1",
                PlantationId: null,
                PlantationName: null,
                CropCycleId: null,
                CropCycleName: null,
                CropCycleStageId: null,
                CropCycleStageName: null,
                Status: IrrigationStatus.Draft,
                StatusName: "Draft",
                IsOverdue: false,
                PlannedAt: null,
                ScheduledAt: null,
                ActualStartedAt: null,
                ActualEndedAt: null,
                ActualDurationMinutes: null,
                IrrigationMethodId: null,
                IrrigationMethodName: null,
                PlannedWaterQuantity: null,
                PlannedWaterUnitId: null,
                PlannedWaterUnitName: null,
                ActualWaterQuantity: null,
                ActualWaterUnitId: null,
                ActualWaterUnitName: null,
                CompletedAt: null,
                CreatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(new PagedResponse<IrrigationListItemResponse>([item], 1, 1, 20));
        }

        public Task<IrrigationDetailsResponse> GetAsync(IrrigationActor actor, Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IrrigationDetailsResponse> CreateDraftAsync(IrrigationActor actor, CreateIrrigationDraftRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(Guid.NewGuid(), request.FarmId, request.FarmAreaId));
        }

        public Task<IrrigationDetailsResponse> UpdateDraftAsync(IrrigationActor actor, Guid id, UpdateIrrigationDraftRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id, request.FarmId, request.FarmAreaId));
        }

        public Task<IrrigationDetailsResponse> ScheduleAsync(IrrigationActor actor, Guid id, ScheduleIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IrrigationDetailsResponse> RescheduleAsync(IrrigationActor actor, Guid id, RescheduleIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IrrigationDetailsResponse> StartAsync(IrrigationActor actor, Guid id, StartIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IrrigationDetailsResponse> CompleteAsync(IrrigationActor actor, Guid id, CompleteIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IrrigationDetailsResponse> RecordCompletedAsync(IrrigationActor actor, RecordCompletedIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(Guid.NewGuid(), request.FarmId, request.FarmAreaId));
        }

        public Task<IrrigationDetailsResponse> CancelAsync(IrrigationActor actor, Guid id, CancelIrrigationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateSampleDetails(id));
        }

        public Task<IReadOnlyList<IrrigationMethodDto>> ListMethodsAsync(IrrigationActor actor, bool activeOnly, CancellationToken cancellationToken = default)
        {
            var method = new IrrigationMethodDto(Guid.NewGuid(), null, "DRIP", "Drip Irrigation", null, 1, true, true);
            return Task.FromResult<IReadOnlyList<IrrigationMethodDto>>([method]);
        }

        public Task<IrrigationSummaryCountsResponse> GetSummaryCountsAsync(IrrigationActor actor, Guid? farmId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IrrigationSummaryCountsResponse(5, 1, 1, 1, 1, 1, 0));
        }

        private static IrrigationDetailsResponse CreateSampleDetails(Guid id, Guid? farmId = null, Guid? farmAreaId = null) =>
            new(
                Id: id,
                OrganizationId: Guid.NewGuid(),
                FarmId: farmId ?? Guid.NewGuid(),
                FarmName: "Test Farm",
                FarmAreaId: farmAreaId ?? Guid.NewGuid(),
                FarmAreaName: "Area 1",
                PlantationId: null,
                PlantationName: null,
                CropCycleId: null,
                CropCycleName: null,
                CropCycleStageId: null,
                CropCycleStageName: null,
                Status: IrrigationStatus.Draft,
                StatusName: "Draft",
                IsOverdue: false,
                PlannedAt: null,
                ScheduledAt: null,
                ActualStartedAt: null,
                ActualEndedAt: null,
                ActualDurationMinutes: null,
                IrrigationMethodId: null,
                IrrigationMethodName: null,
                PlannedWaterQuantity: null,
                PlannedWaterUnitId: null,
                PlannedWaterUnitName: null,
                PlannedWaterUnitSymbol: null,
                ActualWaterQuantity: null,
                ActualWaterUnitId: null,
                ActualWaterUnitName: null,
                ActualWaterUnitSymbol: null,
                Notes: null,
                CancellationReason: null,
                CompletedAt: null,
                CreatedAt: DateTimeOffset.UtcNow,
                CreatedBy: Guid.NewGuid(),
                UpdatedAt: null,
                UpdatedBy: null,
                CreatedByName: "User",
                UpdatedByName: null,
                ConcurrencyToken: "2026-01-01T00:00:00.0000000Z");
    }
}
