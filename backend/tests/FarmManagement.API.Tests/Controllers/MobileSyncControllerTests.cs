using System.Security.Claims;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.DTOs.MobileSync;
using FarmManagement.Application.Interfaces.MobileSync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

/// <summary>
/// Unit tests for MobileSyncController verifying tenant extraction,
/// bootstrap sync handling, and incremental delta sync operations.
/// </summary>
public sealed class MobileSyncControllerTests
{
    private readonly FakeMobileSyncService _fakeSyncService = new();

    private MobileSyncController CreateController(Guid userId, Guid organizationId)
    {
        var controller = new MobileSyncController(_fakeSyncService);
        var httpContext = new DefaultHttpContext();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(AuthorizationConstants.OrganizationIdClaimType, organizationId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task Bootstrap_ReturnsOkWithCompletePayload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var controller = CreateController(userId, organizationId);

        _fakeSyncService.ExpectedPayload = new MobileSyncPayloadResponse(
            DateTimeOffset.UtcNow,
            farmId,
            Farms: [new CachedFarmDto(farmId, organizationId, "Green Valley", null, Guid.NewGuid(), 100, null, null, null, "Fresno", null, "CA", "USA", null, null, null, true)],
            FarmAreas: [],
            CropPlantations: [],
            CropCycles: [],
            CropCycleStages: [],
            Workers: [new CachedWorkerDto(Guid.NewGuid(), organizationId, "John", "Doe", "John Doe", 1, "555-1234", null, null, 1, null, null, null, null, true, 0)],
            WorkerFarmAssignments: [],
            LaborCategories: [],
            Contractors: [],
            LaborWageRates: [],
            Units: [],
            StorageLocations: [],
            InventoryItems: [],
            PlantProtectionProducts: [],
            IrrigationMethods: []
        );

        // Act
        var result = await controller.Bootstrap(farmId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<MobileSyncPayloadResponse>(okResult.Value);

        Assert.Equal(farmId, payload.FarmId);
        Assert.Single(payload.Farms);
        Assert.Equal("Green Valley", payload.Farms[0].Name);
        Assert.Single(payload.Workers);
        Assert.Equal("John Doe", payload.Workers[0].DisplayName);
        Assert.Equal(organizationId, _fakeSyncService.CapturedOrganizationId);
        Assert.Equal(farmId, _fakeSyncService.CapturedFarmId);
    }

    [Fact]
    public async Task Delta_PassesSinceTimestampAndReturnsDeltaPayload()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var sinceUtc = DateTimeOffset.UtcNow.AddHours(-6);
        var controller = CreateController(userId, organizationId);

        _fakeSyncService.ExpectedPayload = new MobileSyncPayloadResponse(
            DateTimeOffset.UtcNow,
            farmId,
            Farms: [],
            FarmAreas: [],
            CropPlantations: [],
            CropCycles: [],
            CropCycleStages: [],
            Workers: [new CachedWorkerDto(Guid.NewGuid(), organizationId, "Jane", "Smith", "Jane Smith", 2, "555-5678", null, null, 1, null, null, null, null, true, 0)],
            WorkerFarmAssignments: [],
            LaborCategories: [],
            Contractors: [],
            LaborWageRates: [],
            Units: [],
            StorageLocations: [],
            InventoryItems: [],
            PlantProtectionProducts: [],
            IrrigationMethods: []
        );

        // Act
        var result = await controller.Delta(sinceUtc, farmId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<MobileSyncPayloadResponse>(okResult.Value);

        Assert.Single(payload.Workers);
        Assert.Equal("Jane Smith", payload.Workers[0].DisplayName);
        Assert.Equal(organizationId, _fakeSyncService.CapturedOrganizationId);
        Assert.Equal(farmId, _fakeSyncService.CapturedFarmId);
        Assert.Equal(sinceUtc, _fakeSyncService.CapturedSinceUtc);
    }

    private sealed class FakeMobileSyncService : IMobileSyncService
    {
        public MobileSyncPayloadResponse? ExpectedPayload { get; set; }
        public Guid CapturedOrganizationId { get; private set; }
        public Guid? CapturedFarmId { get; private set; }
        public DateTimeOffset CapturedSinceUtc { get; private set; }

        public Task<MobileSyncPayloadResponse> GetBootstrapPayloadAsync(
            Guid organizationId,
            Guid? farmId,
            CancellationToken cancellationToken = default)
        {
            CapturedOrganizationId = organizationId;
            CapturedFarmId = farmId;

            return Task.FromResult(ExpectedPayload ?? new MobileSyncPayloadResponse(
                DateTimeOffset.UtcNow,
                farmId,
                [], [], [], [], [], [], [], [], [], [], [], [], [], [], []));
        }

        public Task<MobileSyncPayloadResponse> GetDeltaPayloadAsync(
            Guid organizationId,
            Guid? farmId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default)
        {
            CapturedOrganizationId = organizationId;
            CapturedFarmId = farmId;
            CapturedSinceUtc = sinceUtc;

            return Task.FromResult(ExpectedPayload ?? new MobileSyncPayloadResponse(
                DateTimeOffset.UtcNow,
                farmId,
                [], [], [], [], [], [], [], [], [], [], [], [], [], [], []));
        }
    }
}
