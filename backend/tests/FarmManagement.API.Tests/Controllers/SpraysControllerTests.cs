using System.Security.Claims;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Sprays;
using FarmManagement.Application.Interfaces.Sprays;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

public class SpraysControllerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private SpraysController CreateController(ISprayService service)
    {
        var controller = new SpraysController(service);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new(AuthorizationConstants.OrganizationIdClaimType, _orgId.ToString()),
            new("permissions", "Spray.View")
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
        var fakeService = new FakeSprayService();
        var controller = CreateController(fakeService);
        var query = new SprayListQuery(FarmId: Guid.NewGuid());

        // Act
        var actionResult = await controller.List(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<PagedResponse<SprayListItemResponse>>(okResult.Value);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Get_ReturnsOkWithDetails()
    {
        // Arrange
        var fakeService = new FakeSprayService();
        var controller = CreateController(fakeService);
        var sprayId = Guid.NewGuid();

        // Act
        var actionResult = await controller.Get(sprayId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<SprayDetailsResponse>(okResult.Value);
        Assert.Equal(sprayId, result.Id);
    }

    private sealed class FakeSprayService : ISprayService
    {
        public Task<PagedResponse<SprayListItemResponse>> ListAsync(SprayActor actor, SprayListQuery query, CancellationToken cancellationToken = default)
        {
            var item = new SprayListItemResponse(
                Id: Guid.NewGuid(),
                ReferenceNumber: "SP-12345678",
                FarmId: query.FarmId ?? Guid.NewGuid(),
                FarmName: "Test Farm",
                FarmAreaId: null,
                FarmAreaName: null,
                PlantationId: null,
                PlantationName: null,
                CropCycleId: null,
                CropCycleName: null,
                CropCycleStageId: null,
                CropCycleStageName: null,
                Status: SprayStatus.Scheduled,
                StatusName: "Scheduled",
                IsOverdue: false,
                PlannedDate: null,
                ScheduledDateTime: DateTimeOffset.UtcNow,
                ActualApplicationDateTime: null,
                TargetId: null,
                TargetName: null,
                ApplicationMethodId: null,
                ApplicationMethodName: null,
                ProductCount: 0,
                CreatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(new PagedResponse<SprayListItemResponse>([item], 1, 1, 20));
        }

        public Task<SprayDetailsResponse> GetAsync(SprayActor actor, Guid id, CancellationToken cancellationToken = default)
        {
            var details = new SprayDetailsResponse(
                Id: id,
                ReferenceNumber: "SP-12345678",
                OrganizationId: actor.OrganizationId,
                FarmId: Guid.NewGuid(),
                FarmName: "Test Farm",
                FarmAreaId: null,
                FarmAreaName: null,
                PlantationId: null,
                PlantationName: null,
                CropCycleId: null,
                CropCycleName: null,
                CropCycleStageId: null,
                CropCycleStageName: null,
                Status: SprayStatus.Scheduled,
                StatusName: "Scheduled",
                IsOverdue: false,
                PlannedDate: null,
                ScheduledDateTime: DateTimeOffset.UtcNow,
                ActualApplicationDateTime: null,
                PlannedArea: null,
                PlannedAreaUnitId: null,
                PlannedAreaUnitName: null,
                ActualTreatedArea: null,
                ActualTreatedAreaUnitId: null,
                ActualTreatedAreaUnitName: null,
                WaterQuantity: null,
                WaterUnitId: null,
                WaterUnitName: null,
                TargetId: null,
                TargetName: null,
                TargetType: TargetType.Insect,
                ApplicationMethodId: null,
                ApplicationMethodName: null,
                PurposeReason: null,
                CancellationReason: null,
                Products: [],
                CreatedAt: DateTimeOffset.UtcNow,
                CreatedBy: actor.UserId,
                UpdatedAt: null,
                UpdatedBy: null);

            return Task.FromResult(details);
        }
    }
}
