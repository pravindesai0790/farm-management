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
                "Fertilizers");

            return Task.FromResult<IReadOnlyList<FertilizerProductLookupResponse>>([item]);
        }

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
    }
}
