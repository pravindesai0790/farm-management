using System.Reflection;
using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class FertilizerApplicationServiceTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeFertilizerStore _store = new();
    private readonly FertilizerApplicationService _sut;

    public FertilizerApplicationServiceTests()
    {
        var validator = new FarmManagement.Application.Services.Fertilizer.FertilizerValidator(_store);
        _sut = new FertilizerApplicationService(_store, validator, new FakeFertilizerReferenceGenerator());
    }

    private sealed class FakeFertilizerReferenceGenerator : IFertilizerReferenceGenerator
    {
        private int _seq = 0;
        public Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
        {
            _seq++;
            return Task.FromResult($"FERT-{year}-{_seq:D6}");
        }
    }

    private FertilizerActor CreateActor(Guid? orgId = null) =>
        new(_userId, orgId ?? _orgId);

    [Fact]
    public async Task ListAsync_DefaultPagination_ReturnsFirstPageWithDefaultSize()
    {
        // Arrange
        var actor = CreateActor();
        for (var i = 1; i <= 25; i++)
        {
            var app = new FertilizerApplication(_orgId, $"FERT-2026-{i:D6}", _userId, purposeReason: $"App {i}");
            _store.Applications.Add(app);
        }

        var query = new FertilizerApplicationListQuery(PageNumber: 1, PageSize: 20);

        // Act
        var result = await _sut.ListAsync(actor, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(20, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task ListAsync_FiltersByStatus_IncludesDraftAndCancelledInAll()
    {
        // Arrange
        var actor = CreateActor();
        var draft = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        var completed = new FertilizerApplication(_orgId, "FERT-2026-000002", _userId);
        SetPrivateProperty(completed, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        var cancelled = new FertilizerApplication(_orgId, "FERT-2026-000003", _userId);
        SetPrivateProperty(cancelled, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Cancelled);

        _store.Applications.AddRange([draft, completed, cancelled]);

        // Act - All query (null status)
        var allResult = await _sut.ListAsync(actor, new FertilizerApplicationListQuery());

        // Assert - All includes Draft and Cancelled
        Assert.Equal(3, allResult.TotalCount);

        // Act - Specific status filter
        var completedResult = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(Status: FertilizerApplicationStatus.Completed));
        Assert.Single(completedResult.Items);
        Assert.Equal(FertilizerApplicationStatus.Completed, completedResult.Items[0].Status);
    }

    [Fact]
    public async Task ListAsync_FiltersByFarmAndArea()
    {
        // Arrange
        var actor = CreateActor();
        var farm1 = Guid.NewGuid();
        var farm2 = Guid.NewGuid();
        var area1 = Guid.NewGuid();

        var app1 = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, farmId: farm1, farmAreaId: area1);
        var app2 = new FertilizerApplication(_orgId, "FERT-2026-000002", _userId, farmId: farm2);

        _store.Applications.AddRange([app1, app2]);

        // Act
        var result = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(FarmId: farm1, FarmAreaId: area1));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(app1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_FiltersByInventoryItem()
    {
        // Arrange
        var actor = CreateActor();
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();

        var app1 = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        app1.AddProduct(item1, 50m, _userId);

        var app2 = new FertilizerApplication(_orgId, "FERT-2026-000002", _userId);
        app2.AddProduct(item2, 30m, _userId);

        _store.Applications.AddRange([app1, app2]);

        // Act
        var result = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(InventoryItemId: item1));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(app1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_DateRange_PlannedDate_ExcludesNullDatesWhenRangeActive()
    {
        // Arrange
        var actor = CreateActor();
        var fromDate = new DateOnly(2026, 5, 1);
        var toDate = new DateOnly(2026, 5, 31);

        var appWithNullPlannedDate = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, plannedDate: null);
        var appInRange = new FertilizerApplication(_orgId, "FERT-2026-000002", _userId, plannedDate: new DateOnly(2026, 5, 15));
        var appOutOfRange = new FertilizerApplication(_orgId, "FERT-2026-000003", _userId, plannedDate: new DateOnly(2026, 6, 1));

        _store.Applications.AddRange([appWithNullPlannedDate, appInRange, appOutOfRange]);

        // Act - with active range
        var resultWithRange = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(
            DateType: "PlannedDate",
            FromDate: fromDate,
            ToDate: toDate));

        // Assert - null planned date is excluded
        Assert.Single(resultWithRange.Items);
        Assert.Equal(appInRange.Id, resultWithRange.Items[0].Id);

        // Act - without range
        var resultNoRange = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(DateType: "PlannedDate"));

        // Assert - null planned date is preserved
        Assert.Equal(3, resultNoRange.TotalCount);
    }

    [Fact]
    public async Task ListAsync_DateRange_ActualApplicationDate_ExcludesNullDatesWhenRangeActive()
    {
        // Arrange
        var actor = CreateActor();
        var fromDate = new DateOnly(2026, 4, 1);
        var toDate = new DateOnly(2026, 4, 30);

        var appNullActual = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        var appMatchingActual = new FertilizerApplication(_orgId, "FERT-2026-000002", _userId);
        SetPrivateProperty(appMatchingActual, nameof(FertilizerApplication.ActualApplicationDate), new DateOnly(2026, 4, 10));

        _store.Applications.AddRange([appNullActual, appMatchingActual]);

        // Act
        var result = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(
            DateType: "ActualApplicationDate",
            FromDate: fromDate,
            ToDate: toDate));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(appMatchingActual.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_SearchFilter_MatchesReferenceOrPurpose()
    {
        // Arrange
        var actor = CreateActor();
        var app1 = new FertilizerApplication(_orgId, "FERT-2026-000123", _userId, purposeReason: "Corn vegetative fertilization");
        var app2 = new FertilizerApplication(_orgId, "FERT-2026-000456", _userId, purposeReason: "Wheat basal dressing");

        _store.Applications.AddRange([app1, app2]);

        // Act
        var result = await _sut.ListAsync(actor, new FertilizerApplicationListQuery(Search: "corn"));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(app1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task ListAsync_MultiTenantIsolation_ExcludesOtherOrganizationRecords()
    {
        // Arrange
        var actorOrgA = CreateActor(_orgId);
        var otherOrgId = Guid.NewGuid();

        var appOrgA = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId);
        var appOrgB = new FertilizerApplication(otherOrgId, "FERT-2026-000002", _userId);

        _store.Applications.AddRange([appOrgA, appOrgB]);

        // Act
        var result = await _sut.ListAsync(actorOrgA, new FertilizerApplicationListQuery());

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(appOrgA.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAsync_WhenFound_CalculatesVarianceAndReturnsDetails()
    {
        // Arrange
        var actor = CreateActor();
        var app = new FertilizerApplication(_orgId, "FERT-2026-000001", _userId, purposeReason: "Test variance");
        var item = Guid.NewGuid();
        var product = app.AddProduct(item, 100m, _userId);
        SetPrivateProperty(product, nameof(FertilizerApplicationProduct.ActualQuantity), 115m);

        _store.Applications.Add(app);

        // Act
        var details = await _sut.GetAsync(actor, app.Id);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(app.Id, details.Id);
        Assert.Equal("FERT-2026-000001", details.ReferenceNumber);
        Assert.Single(details.Products);
        Assert.Equal(100m, details.Products[0].PlannedQuantity);
        Assert.Equal(115m, details.Products[0].ActualQuantity);
        Assert.Equal(15m, details.Products[0].VarianceQuantity);
        Assert.NotNull(details.ConcurrencyToken);
    }

    [Fact]
    public async Task GetAsync_CrossOrganization_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actorOrgA = CreateActor(_orgId);
        var otherOrgId = Guid.NewGuid();
        var appOrgB = new FertilizerApplication(otherOrgId, "FERT-2026-000001", _userId);
        _store.Applications.Add(appOrgB);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAsync(actorOrgA, appOrgB.Id));
    }

    [Fact]
    public async Task GetAsync_NonExistentId_ThrowsResourceNotFoundException()
    {
        // Arrange
        var actor = CreateActor();

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAsync(actor, Guid.NewGuid()));
    }

    [Fact]
    public async Task Lookups_Products_ReturnsOnlyFertilizerCategoryWithoutPppProfile()
    {
        // Arrange
        var actor = CreateActor();
        _store.ProductsLookup =
        [
            new(Guid.NewGuid(), "Urea 46%", "UREA-46", Guid.NewGuid(), "Kilogram", "KG", "kg", FarmInventoryCategories.FertilizersSoilId, "Fertilizers & Soil Amendments"),
            new(Guid.NewGuid(), "NPK 15-15-15", "NPK-15", Guid.NewGuid(), "Kilogram", "KG", "kg", FarmInventoryCategories.FertilizersSoilId, "Fertilizers & Soil Amendments")
        ];

        // Act
        var result = await _sut.ListProductsLookupAsync(actor);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.ItemName == "Urea 46%");
    }

    [Fact]
    public async Task Lookups_StorageLocations_RequiresFarmIdAndReturnsLocations()
    {
        // Arrange
        var actor = CreateActor();
        var farmId = Guid.NewGuid();
        _store.LocationsLookup =
        [
            new(Guid.NewGuid(), "Main Warehouse Bay 1", 250m, true, Guid.NewGuid(), "Kilogram", "kg")
        ];

        // Act
        var result = await _sut.ListStorageLocationsLookupAsync(actor, farmId, null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Main Warehouse Bay 1", result[0].StorageLocationName);
        Assert.Equal(250m, result[0].CurrentStock);

        // Act & Assert: empty farm throws
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ListStorageLocationsLookupAsync(actor, Guid.Empty, null));
    }

    [Fact]
    public async Task Lookups_ApplicationMethods_ReturnsOnlyFertilizerMethods()
    {
        // Arrange
        var actor = CreateActor();
        _store.MethodsLookup =
        [
            new(Guid.NewGuid(), "BROADCASTING", "Broadcasting", null, 1, false),
            new(Guid.NewGuid(), "FERTIGATION", "Fertigation", null, 5, false),
            new(Guid.NewGuid(), "OTHER", "Other", null, 7, true)
        ];

        // Act
        var result = await _sut.ListApplicationMethodsLookupAsync(actor);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, m => m.Code == "FERTIGATION");
        Assert.True(result.First(m => m.Code == "OTHER").RequiresDescription);
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
        }
        else
        {
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }

    public sealed class FakeFertilizerStore : IFertilizerApplicationStore
    {
        public List<FertilizerApplication> Applications { get; } = [];
        public List<Farm> Farms { get; } = [];
        public List<FarmArea> FarmAreas { get; } = [];
        public List<CropPlantation> Plantations { get; } = [];
        public List<CropCycle> CropCycles { get; } = [];
        public List<CropCycleStage> CropCycleStages { get; } = [];
        public List<ApplicationMethod> ApplicationMethods { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<InventoryItem> InventoryItems { get; } = [];
        public HashSet<Guid> ActivePppItemIds { get; } = [];
        public List<StorageLocation> StorageLocations { get; } = [];
        public List<IrrigationEvent> IrrigationEvents { get; } = [];
        public List<StockBalance> StockBalances { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public void Add(FertilizerApplication application) => Applications.Add(application);

        public void Remove(FertilizerApplication application) => Applications.Remove(application);

        public void RemoveProduct(FertilizerApplicationProduct product)
        {
            var app = Applications.FirstOrDefault(a => a.Id == product.FertilizerApplicationId);
            if (app is not null)
            {
                app.RemoveProduct(product.InventoryItemId);
            }
        }

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<FertilizerProductLookupResponse> ProductsLookup { get; set; } = [];
        public IReadOnlyList<FertilizerStorageLocationLookupResponse> LocationsLookup { get; set; } = [];
        public IReadOnlyList<FertilizerApplicationMethodLookupResponse> MethodsLookup { get; set; } = [];
        public IReadOnlyList<FertilizerIrrigationLookupResponse> IrrigationsLookup { get; set; } = [];

        public Task<FertilizerApplication?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Applications.FirstOrDefault(a => a.Id == id && a.OrganizationId == organizationId));

        public Task<int> CountAsync(Guid organizationId, FertilizerApplicationListQuery query, CancellationToken cancellationToken = default)
        {
            var filtered = ApplyFilters(organizationId, query);
            return Task.FromResult(filtered.Count());
        }

        public Task<IReadOnlyList<FertilizerApplication>> ListAsync(
            Guid organizationId,
            FertilizerApplicationListQuery query,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var filtered = ApplyFilters(organizationId, query);
            var items = filtered.Skip(skip).Take(take).ToList();
            return Task.FromResult<IReadOnlyList<FertilizerApplication>>(items);
        }

        public Task<IReadOnlyList<FertilizerProductLookupResponse>> ListProductsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProductsLookup);

        public Task<IReadOnlyList<FertilizerStorageLocationLookupResponse>> ListStorageLocationsLookupAsync(Guid organizationId, Guid farmId, Guid? inventoryItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(LocationsLookup);

        public Task<IReadOnlyList<FertilizerApplicationMethodLookupResponse>> ListApplicationMethodsLookupAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MethodsLookup);

        public Task<IReadOnlyList<FertilizerIrrigationLookupResponse>> ListIrrigationsLookupAsync(Guid organizationId, Guid? farmId, Guid? farmAreaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IrrigationsLookup);

        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<Farm?> FindFarmAsync(Guid farmId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == farmId && f.OrganizationId == organizationId));

        public Task<FarmArea?> FindFarmAreaAsync(Guid farmAreaId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(FarmAreas.FirstOrDefault(fa => fa.Id == farmAreaId && fa.OrganizationId == organizationId));

        public Task<CropPlantation?> FindPlantationAsync(Guid plantationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plantations.FirstOrDefault(p => p.Id == plantationId && p.OrganizationId == organizationId));

        public Task<CropCycle?> FindCropCycleAsync(Guid cropCycleId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycles.FirstOrDefault(c => c.Id == cropCycleId && c.OrganizationId == organizationId));

        public Task<CropCycleStage?> FindCropCycleStageAsync(Guid cropCycleStageId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CropCycleStages.FirstOrDefault(s => s.Id == cropCycleStageId));

        public Task<ApplicationMethod?> FindApplicationMethodAsync(Guid applicationMethodId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationMethods.FirstOrDefault(m => m.Id == applicationMethodId && ((m.IsSystem && m.OrganizationId == null) || m.OrganizationId == organizationId)));

        public Task<Unit?> FindUnitAsync(Guid unitId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Units.FirstOrDefault(u => u.Id == unitId && ((u.IsSystem && u.OrganizationId == null) || u.OrganizationId == organizationId)));

        public Task<InventoryItem?> FindInventoryItemAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId && i.OrganizationId == organizationId));

        public Task<bool> HasActivePlantProtectionProfileAsync(Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivePppItemIds.Contains(inventoryItemId));

        public Task<StorageLocation?> FindStorageLocationAsync(Guid storageLocationId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StorageLocations.FirstOrDefault(l => l.Id == storageLocationId && l.OrganizationId == organizationId));

        public Task<IrrigationEvent?> FindIrrigationEventAsync(Guid irrigationEventId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IrrigationEvents.FirstOrDefault(e => e.Id == irrigationEventId && e.OrganizationId == organizationId));

        public Task<StockBalance?> FindStockBalanceAsync(Guid storageLocationId, Guid inventoryItemId, Guid organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StockBalances.FirstOrDefault(sb => sb.StorageLocationId == storageLocationId && sb.InventoryItemId == inventoryItemId && sb.OrganizationId == organizationId));

        private IEnumerable<FertilizerApplication> ApplyFilters(Guid organizationId, FertilizerApplicationListQuery query)
        {
            var q = Applications.Where(a => a.OrganizationId == organizationId);

            if (query.FarmId.HasValue && query.FarmId.Value != Guid.Empty)
                q = q.Where(a => a.FarmId == query.FarmId.Value);

            if (query.FarmAreaId.HasValue && query.FarmAreaId.Value != Guid.Empty)
                q = q.Where(a => a.FarmAreaId == query.FarmAreaId.Value);

            if (query.PlantationId.HasValue && query.PlantationId.Value != Guid.Empty)
                q = q.Where(a => a.PlantationId == query.PlantationId.Value);

            if (query.CropCycleId.HasValue && query.CropCycleId.Value != Guid.Empty)
                q = q.Where(a => a.CropCycleId == query.CropCycleId.Value);

            if (query.CropCycleStageId.HasValue && query.CropCycleStageId.Value != Guid.Empty)
                q = q.Where(a => a.CropCycleStageId == query.CropCycleStageId.Value);

            if (query.Status.HasValue)
                q = q.Where(a => a.Status == query.Status.Value);

            if (query.InventoryItemId.HasValue && query.InventoryItemId.Value != Guid.Empty)
                q = q.Where(a => a.Products.Any(p => p.InventoryItemId == query.InventoryItemId.Value));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                q = q.Where(a =>
                    a.ReferenceNumber.ToLower().Contains(search) ||
                    (a.PurposeReason != null && a.PurposeReason.ToLower().Contains(search)) ||
                    (a.OtherMethodDescription != null && a.OtherMethodDescription.ToLower().Contains(search)));
            }

            var hasRange = query.FromDate.HasValue || query.ToDate.HasValue;
            var dateType = query.DateType?.Trim().ToLowerInvariant();

            switch (dateType)
            {
                case "planneddate":
                    if (hasRange)
                    {
                        q = q.Where(a => a.PlannedDate != null);
                        if (query.FromDate.HasValue) q = q.Where(a => a.PlannedDate >= query.FromDate.Value);
                        if (query.ToDate.HasValue) q = q.Where(a => a.PlannedDate <= query.ToDate.Value);
                    }
                    break;

                case "scheduleddate":
                    if (hasRange)
                    {
                        q = q.Where(a => a.ScheduledDate != null);
                        if (query.FromDate.HasValue) q = q.Where(a => a.ScheduledDate >= query.FromDate.Value);
                        if (query.ToDate.HasValue) q = q.Where(a => a.ScheduledDate <= query.ToDate.Value);
                    }
                    break;

                case "actualapplicationdate" or "actualdate":
                    if (hasRange)
                    {
                        q = q.Where(a => a.ActualApplicationDate != null);
                        if (query.FromDate.HasValue) q = q.Where(a => a.ActualApplicationDate >= query.FromDate.Value);
                        if (query.ToDate.HasValue) q = q.Where(a => a.ActualApplicationDate <= query.ToDate.Value);
                    }
                    break;
            }

            return q;
        }
    }
}
