using System.Security.Claims;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

public class FertilizerApplicationsControllerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private FertilizerApplicationsController CreateController(IFertilizerApplicationService service)
    {
        var controller = new FertilizerApplicationsController(service);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new(AuthorizationConstants.OrganizationIdClaimType, _orgId.ToString()),
            new("permissions", "Fertilizer.View")
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
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var query = new FertilizerApplicationListQuery();

        // Act
        var actionResult = await controller.List(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<PagedResponse<FertilizerApplicationListItemResponse>>(okResult.Value);
        Assert.Single(result.Items);
        Assert.Equal("FERT-2026-000001", result.Items[0].ReferenceNumber);
    }

    [Fact]
    public async Task Get_ReturnsOkWithDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();

        // Act
        var actionResult = await controller.Get(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal("FERT-2026-000001", result.ReferenceNumber);
    }

    [Fact]
    public async Task ListProductsLookup_ReturnsOkWithProducts()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);

        // Act
        var actionResult = await controller.ListProductsLookup();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsAssignableFrom<IReadOnlyList<FertilizerProductLookupResponse>>(okResult.Value);
        Assert.Single(result);
        Assert.Equal("Urea 46%", result[0].ItemName);
    }

    [Fact]
    public async Task ListStorageLocationsLookup_ReturnsOkWithLocations()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var farmId = Guid.NewGuid();

        // Act
        var actionResult = await controller.ListStorageLocationsLookup(farmId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsAssignableFrom<IReadOnlyList<FertilizerStorageLocationLookupResponse>>(okResult.Value);
        Assert.Single(result);
        Assert.Equal("Main Storage", result[0].StorageLocationName);
    }

    [Fact]
    public async Task ListApplicationMethodsLookup_ReturnsOkWithMethods()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);

        // Act
        var actionResult = await controller.ListApplicationMethodsLookup();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsAssignableFrom<IReadOnlyList<FertilizerApplicationMethodLookupResponse>>(okResult.Value);
        Assert.Single(result);
        Assert.Equal("BROADCASTING", result[0].Code);
    }

    [Fact]
    public async Task ListIrrigationsLookup_ReturnsOkWithIrrigations()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);

        // Act
        var actionResult = await controller.ListIrrigationsLookup(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsAssignableFrom<IReadOnlyList<FertilizerIrrigationLookupResponse>>(okResult.Value);
        Assert.Single(result);
        Assert.Equal("Area North", result[0].FarmAreaName);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var request = new CreateFertilizerDraftRequest(PurposeReason: "Spring fertilization");

        // Act
        var actionResult = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(createdResult.Value);
        Assert.Equal("FERT-2026-000001", result.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, result.Status);
    }

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new UpdateFertilizerDraftRequest(PurposeReason: "Updated draft", Version: 1);

        // Act
        var actionResult = await controller.Update(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(2u, result.Version);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();

        // Act
        var actionResult = await controller.Delete(id);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Copy_ReturnsCreatedWithNewDraftDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var sourceId = Guid.NewGuid();

        // Act
        var actionResult = await controller.Copy(sourceId);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(createdResult.Value);
        Assert.NotEqual(sourceId, result.Id);
        Assert.Equal("FERT-2026-000002", result.ReferenceNumber);
        Assert.Equal(FertilizerApplicationStatus.Draft, result.Status);
    }

    [Fact]
    public async Task Plan_ReturnsOkWithPlannedDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new PlanFertilizerApplicationRequest(PlannedDate: DateTimeOffset.UtcNow.AddDays(1));

        // Act
        var actionResult = await controller.Plan(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.Planned, result.Status);
    }

    [Fact]
    public async Task Schedule_ReturnsOkWithScheduledDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new ScheduleFertilizerApplicationRequest(DateTimeOffset.UtcNow.AddDays(2));

        // Act
        var actionResult = await controller.Schedule(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.Scheduled, result.Status);
    }

    [Fact]
    public async Task Reschedule_ReturnsOkWithUpdatedScheduledDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new RescheduleFertilizerApplicationRequest(DateTimeOffset.UtcNow.AddDays(5));

        // Act
        var actionResult = await controller.Reschedule(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.Scheduled, result.Status);
    }

    [Fact]
    public async Task Start_ReturnsOkWithInProgressDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new StartFertilizerApplicationRequest();

        // Act
        var actionResult = await controller.Start(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task Complete_ReturnsOkWithCompletedDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new CompleteFertilizerApplicationRequest(DateTimeOffset.UtcNow);

        // Act
        var actionResult = await controller.Complete(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
    }

    [Fact]
    public async Task Cancel_ReturnsOkWithCancelledDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new CancelFertilizerApplicationRequest("Adverse weather");

        // Act
        var actionResult = await controller.Cancel(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(okResult.Value);
        Assert.Equal(id, result.Id);
        Assert.Equal(FertilizerApplicationStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task RecordCompleted_ReturnsCreatedWithDetails()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var farmId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var methodId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var locId = Guid.NewGuid();

        var request = new RecordCompletedFertilizerApplicationRequest(
            FarmId: farmId,
            FarmAreaId: areaId,
            ApplicationMethodId: methodId,
            ActualApplicationDate: DateTimeOffset.UtcNow,
            Products: [new(itemId, locId, 50m)]);

        // Act
        var actionResult = await controller.RecordCompleted(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var result = Assert.IsType<FertilizerApplicationDetailsResponse>(createdResult.Value);
        Assert.Equal(FertilizerApplicationStatus.Completed, result.Status);
        Assert.Equal("COMPLETED", result.StatusName);
    }

    [Fact]
    public async Task ExportCsv_ReturnsFileResult_WithCsvContentTypeAndFileName()
    {
        // Arrange
        var fakeService = new FakeFertilizerApplicationService();
        var controller = CreateController(fakeService);
        var query = new FertilizerApplicationListQuery();

        // Act
        var actionResult = await controller.ExportCsv(query);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.StartsWith("fertilizer-applications-", fileResult.FileDownloadName);
        Assert.EndsWith(".csv", fileResult.FileDownloadName);
        Assert.NotEmpty(fileResult.FileContents);
    }

    private sealed class FakeFertilizerApplicationService : IFertilizerApplicationService
    {
        public Task<PagedResponse<FertilizerApplicationListItemResponse>> ListAsync(
            FertilizerActor actor,
            FertilizerApplicationListQuery query,
            CancellationToken cancellationToken = default)
        {
            var item = new FertilizerApplicationListItemResponse(
                Guid.NewGuid(),
                "FERT-2026-000001",
                null, null, null, null, null, null, null, null, null, null,
                FertilizerApplicationStatus.Draft,
                "DRAFT",
                null, null, null, null,
                null, null, null,
                null, null, null,
                null, null, null,
                "Test draft",
                0,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                "Test User");

            var response = new PagedResponse<FertilizerApplicationListItemResponse>([item], 1, 20, 1);
            return Task.FromResult(response);
        }

        public Task<byte[]> ExportCsvAsync(
            FertilizerActor actor,
            FertilizerApplicationListQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(System.Text.Encoding.UTF8.GetBytes("Reference Number,Status\n"));
        }

        public Task<FertilizerApplicationDetailsResponse> GetAsync(
            FertilizerActor actor,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var details = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Draft,
                "DRAFT",
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null,
                null, null, null,
                null, null, null,
                null, null, null,
                "Test draft",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Test User", null, null, null, null,
                1,
                "1");

            return Task.FromResult(details);
        }

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
            FertilizerActor actor,
            Guid? storageLocationId = null,
            Guid? farmId = null,
            CancellationToken cancellationToken = default)
        {
            var item = new FertilizerProductLookupResponse(
                Guid.NewGuid(),
                "Urea 46%",
                "UREA-46",
                Guid.NewGuid(),
                "Kilogram",
                "KG",
                "kg",
                Guid.NewGuid(),
                "Fertilizers",
                storageLocationId.HasValue ? 100m : 0m,
                storageLocationId.HasValue);

            return Task.FromResult<IReadOnlyList<FertilizerProductLookupResponse>>([item]);
        }

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(
            FertilizerActor actor,
            CancellationToken cancellationToken = default)
            => ListProductsLookupAsync(actor, null, null, cancellationToken);

        public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(
            FertilizerActor actor,
            Guid farmId,
            Guid? inventoryItemId,
            CancellationToken cancellationToken = default)
        {
            var item = new FertilizerStorageLocationLookupResponse(
                Guid.NewGuid(),
                "Main Storage",
                500m,
                true,
                Guid.NewGuid(),
                "Kilogram",
                "kg");

            return Task.FromResult<IReadOnlyList<FertilizerStorageLocationLookupResponse>>([item]);
        }

        public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(
            FertilizerActor actor,
            CancellationToken cancellationToken = default)
        {
            var method = new FertilizerApplicationMethodLookupResponse(
                Guid.NewGuid(),
                "BROADCASTING",
                "Broadcasting",
                "Broadcast application",
                1,
                false);

            return Task.FromResult<IReadOnlyList<FertilizerApplicationMethodLookupResponse>>([method]);
        }

        public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(
            FertilizerActor actor,
            Guid? farmId,
            Guid? farmAreaId,
            CancellationToken cancellationToken = default)
        {
            var irrigation = new FertilizerIrrigationLookupResponse(
                Guid.NewGuid(),
                farmId ?? Guid.NewGuid(),
                farmAreaId ?? Guid.NewGuid(),
                "Area North",
                null, null,
                "Completed",
                null, null,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(-1),
                "Drip",
                5000m,
                "L");

            return Task.FromResult<IReadOnlyList<FertilizerIrrigationLookupResponse>>([irrigation]);
        }

        public Task<FertilizerApplicationDetailsResponse> CreateDraftAsync(
            FertilizerActor actor,
            CreateFertilizerDraftRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Draft,
                "DRAFT",
                request.FarmId,
                null,
                request.FarmAreaId,
                null,
                request.PlantationId,
                null,
                request.CropCycleId,
                null,
                request.CropCycleStageId,
                null,
                request.ApplicationMethodId,
                null,
                request.OtherMethodDescription,
                request.IrrigationEventId,
                null,
                request.PlannedDate,
                request.ScheduledDate,
                null,
                request.PlannedArea,
                request.PlannedAreaUnitId,
                null,
                null,
                null,
                null,
                request.PurposeReason,
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Admin", null, null, null, null,
                1, "1");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> UpdateDraftAsync(
            FertilizerActor actor,
            Guid id,
            UpdateFertilizerDraftRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Draft,
                "DRAFT",
                request.FarmId,
                null,
                request.FarmAreaId,
                null,
                request.PlantationId,
                null,
                request.CropCycleId,
                null,
                request.CropCycleStageId,
                null,
                request.ApplicationMethodId,
                null,
                request.OtherMethodDescription,
                request.IrrigationEventId,
                null,
                request.PlannedDate,
                request.ScheduledDate,
                null,
                request.PlannedArea,
                request.PlannedAreaUnitId,
                null,
                null,
                null,
                null,
                request.PurposeReason,
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null,
                "Admin", "Admin", null, null, null,
                2, "2");

            return Task.FromResult(response);
        }

        public Task DeleteDraftAsync(
            FertilizerActor actor,
            Guid id,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<FertilizerApplicationDetailsResponse> CopyAsNewDraftAsync(
            FertilizerActor actor,
            Guid sourceId,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var newId = Guid.NewGuid();
            var response = new FertilizerApplicationDetailsResponse(
                newId,
                actor.OrganizationId,
                "FERT-2026-000002",
                FertilizerApplicationStatus.Draft,
                "DRAFT",
                null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null, null, null, null, null,
                "Copied application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Admin", null, null, null, null,
                1, "1");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> PlanAsync(
            FertilizerActor actor,
            Guid id,
            PlanFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Planned,
                "PLANNED",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                DateTimeOffset.UtcNow, null, null,
                null, null, null, null, null, null,
                "Planned application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Admin", null, null, null, null,
                2, "2");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> ScheduleAsync(
            FertilizerActor actor,
            Guid id,
            ScheduleFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Scheduled,
                "SCHEDULED",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                DateTimeOffset.UtcNow, request.ScheduledDate, null,
                null, null, null, null, null, null,
                "Scheduled application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Admin", null, null, null, null,
                2, "2");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> RescheduleAsync(
            FertilizerActor actor,
            Guid id,
            RescheduleFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Scheduled,
                "SCHEDULED",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                DateTimeOffset.UtcNow, request.ScheduledDate, null,
                null, null, null, null, null, null,
                "Rescheduled application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, null, null,
                "Admin", null, null, null, null,
                3, "3");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> StartAsync(
            FertilizerActor actor,
            Guid id,
            StartFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.InProgress,
                "IN_PROGRESS",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                DateTimeOffset.UtcNow, null, null,
                null, null, null, null, null, null,
                "Started application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, DateTimeOffset.UtcNow, actor.UserId, null, null, null, null,
                "Admin", null, "Admin", null, null,
                3, "3");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> CompleteAsync(
            FertilizerActor actor,
            Guid id,
            CompleteFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Completed,
                "COMPLETED",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                null, null, request.ActualApplicationDate,
                null, null, null, request.ActualArea, request.ActualAreaUnitId, null,
                "Completed application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, DateTimeOffset.UtcNow, actor.UserId, null, null,
                "Admin", null, null, "Admin", null,
                4, "4");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> CancelAsync(
            FertilizerActor actor,
            Guid id,
            CancelFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Cancelled,
                "CANCELLED",
                Guid.NewGuid(), null, Guid.NewGuid(), null, null, null, null, null, null, null,
                Guid.NewGuid(), null, null, null, null,
                null, null, request.ActualApplicationDate,
                null, null, null, null, null, null,
                "Cancelled application",
                request.CancellationReason,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, null, null, DateTimeOffset.UtcNow, actor.UserId,
                "Admin", null, null, null, "Admin",
                5, "5");

            return Task.FromResult(response);
        }

        public Task<FertilizerApplicationDetailsResponse> RecordCompletedAsync(
            FertilizerActor actor,
            RecordCompletedFertilizerApplicationRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var response = new FertilizerApplicationDetailsResponse(
                id,
                actor.OrganizationId,
                "FERT-2026-000001",
                FertilizerApplicationStatus.Completed,
                "COMPLETED",
                request.FarmId, null, request.FarmAreaId, null, null, null, null, null, null, null,
                request.ApplicationMethodId, null, null, null, null,
                null, null, request.ActualApplicationDate,
                null, null, null, request.ActualArea, request.ActualAreaUnitId, null,
                "Direct completed application",
                null,
                [],
                DateTimeOffset.UtcNow,
                actor.UserId,
                null, null, null, null, DateTimeOffset.UtcNow, actor.UserId, null, null,
                "Admin", null, null, "Admin", null,
                1, "1");

            return Task.FromResult(response);
        }
    }
}
