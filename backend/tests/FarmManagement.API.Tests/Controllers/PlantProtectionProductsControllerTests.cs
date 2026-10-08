using System.Security.Claims;
using FarmManagement.API.Controllers;
using FarmManagement.Application.Common.Constants;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.PlantProtection;
using FarmManagement.Application.Interfaces.PlantProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FarmManagement.API.Tests.Controllers;

public class PlantProtectionProductsControllerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _organizationId = Guid.NewGuid();

    private PlantProtectionProductsController CreateController(IPlantProtectionProductService service)
    {
        var controller = new PlantProtectionProductsController(service);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new(AuthorizationConstants.OrganizationIdClaimType, _organizationId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };

        return controller;
    }

    [Fact]
    public async Task List_ReturnsOkWithPagedResponse()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService();
        var controller = CreateController(fakeService);

        // Act
        var result = await controller.List(1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsAssignableFrom<PagedResponse<PlantProtectionProductResponse>>(okResult.Value);
        Assert.NotNull(pagedResponse);
    }

    [Fact]
    public async Task Get_ReturnsOkWithResponse()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();

        // Act
        var result = await controller.Get(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PlantProtectionProductResponse>(okResult.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService();
        var controller = CreateController(fakeService);
        var request = new CreatePlantProtectionProductRequest(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PlantProtectionProductsController.Get), createdResult.ActionName);
        var response = Assert.IsType<PlantProtectionProductResponse>(createdResult.Value);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService();
        var controller = CreateController(fakeService);
        var id = Guid.NewGuid();
        var request = new UpdatePlantProtectionProductRequest(Guid.NewGuid());

        // Act
        var result = await controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PlantProtectionProductResponse>(okResult.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task Activate_ReturnsNoContent_WhenTrue()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService { ActivateResult = true };
        var controller = CreateController(fakeService);

        // Act
        var result = await controller.Activate(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Activate_ReturnsBadRequest_WhenFalse()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService { ActivateResult = false };
        var controller = CreateController(fakeService);

        // Act
        var result = await controller.Activate(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContent_WhenTrue()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService { DeactivateResult = true };
        var controller = CreateController(fakeService);

        // Act
        var result = await controller.Deactivate(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Deactivate_ReturnsBadRequest_WhenFalse()
    {
        // Arrange
        var fakeService = new FakePlantProtectionProductService { DeactivateResult = false };
        var controller = CreateController(fakeService);

        // Act
        var result = await controller.Deactivate(Guid.NewGuid(), CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    private sealed class FakePlantProtectionProductService : IPlantProtectionProductService
    {
        public bool ActivateResult { get; set; } = true;
        public bool DeactivateResult { get; set; } = true;

        public Task<PagedResponse<PlantProtectionProductResponse>> ListAsync(
            PlantProtectionActor actor,
            int page,
            int pageSize,
            string? search,
            Guid? productTypeId,
            bool? isActive,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResponse<PlantProtectionProductResponse>([], page, pageSize, 0));

        public Task<PlantProtectionProductResponse> GetAsync(
            PlantProtectionActor actor,
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDummyResponse(id));

        public Task<PlantProtectionProductResponse> CreateAsync(
            PlantProtectionActor actor,
            CreatePlantProtectionProductRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDummyResponse(Guid.NewGuid()));

        public Task<PlantProtectionProductResponse> UpdateAsync(
            PlantProtectionActor actor,
            Guid id,
            UpdatePlantProtectionProductRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDummyResponse(id));

        public Task<bool> ActivateAsync(
            PlantProtectionActor actor,
            Guid id,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivateResult);

        public Task<bool> DeactivateAsync(
            PlantProtectionActor actor,
            Guid id,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DeactivateResult);

        private static PlantProtectionProductResponse CreateDummyResponse(Guid id) =>
            new(
                Id: id,
                OrganizationId: Guid.NewGuid(),
                InventoryItemId: Guid.NewGuid(),
                InventoryItemName: "Item",
                InventoryItemSku: "SKU",
                StockUnitId: Guid.NewGuid(),
                StockUnitCode: "L",
                StockUnitName: "Liter",
                StockUnitSymbol: "L",
                ProductTypeId: Guid.NewGuid(),
                ProductTypeCode: "FUNGICIDE",
                ProductTypeName: "Fungicide",
                ActiveIngredient: null,
                Manufacturer: null,
                Description: null,
                IsActive: true,
                HasCompletedSprayUsage: false,
                CreatedAt: DateTimeOffset.UtcNow,
                CreatedBy: Guid.NewGuid(),
                UpdatedAt: null,
                UpdatedBy: null);
    }
}
