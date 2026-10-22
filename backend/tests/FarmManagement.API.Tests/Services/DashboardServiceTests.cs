using FarmManagement.Application.DTOs.Dashboard;
using FarmManagement.Application.Interfaces.Dashboard;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class DashboardServiceTests
{
    private readonly FakeDashboardStore _store = new();
    private readonly DashboardService _service;
    private readonly DashboardActor _actor = new(Guid.NewGuid(), Guid.NewGuid());

    public DashboardServiceTests()
    {
        _service = new DashboardService(_store);
    }

    [Fact]
    public async Task GetSummary_WhenOrganizationEmpty_ThrowsUnauthorizedAccessException()
    {
        var invalidActor = new DashboardActor(Guid.NewGuid(), Guid.Empty);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetSummaryAsync(invalidActor, null));
    }

    [Fact]
    public async Task GetPriorities_WhenActivitiesOverdue_CalculatesCorrectPriorities()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var farmId = Guid.NewGuid();

        var sprays = new List<RawPrioritySpray>
        {
            new(Guid.NewGuid(), "Powdery Mildew", farmId, "Farm A", "Block 1", "Cycle 1",
                today.AddDays(-2), null, null, "SCHEDULED", null)
        };

        var irrigations = new List<RawPriorityIrrigation>
        {
            new(Guid.NewGuid(), farmId, "Farm A", "Block 1", "Cycle 1",
                now.AddDays(-1), null, null, "DRAFT", 100m, "L")
        };

        var fertilizers = new List<RawPriorityFertilizer>
        {
            new(Guid.NewGuid(), "FA-001", farmId, "Farm A", "Block 1", "Cycle 1",
                now, null, null, "SCHEDULED", "Drip")
        };

        var laborActivities = new List<RawPriorityLaborActivity>
        {
            new(Guid.NewGuid(), "Weeding", farmId, "Farm A", "Block 1", "Cycle 1",
                today.AddDays(-1), "DRAFT")
        };

        var overdueInvoices = new List<RawPriorityOverdueInvoice>
        {
            new(Guid.NewGuid(), "INV-100", "Supplier A", farmId, "Farm A",
                today.AddDays(-5), 5000m, 2000m, 3000m, "₹", "INR")
        };

        var laborData = new LaborDashboardRawData(
            new List<RawLaborAttendanceItem>(),
            new Dictionary<Guid, int>(),
            10
        );

        _store.PrioritiesRawData = new DashboardPrioritiesRawData(
            sprays,
            irrigations,
            fertilizers,
            laborActivities,
            overdueInvoices,
            laborData
        );

        var result = await _service.GetPrioritiesAsync(_actor, farmId);

        Assert.NotNull(result);
        Assert.True(result.TotalAlertsCount > 0);
        Assert.Equal(3, result.OverdueActivities.Count);
        Assert.Single(result.TodayActivities);
        Assert.Equal(1, result.OverdueInvoicesCount);
        Assert.Equal(3000m, result.OverdueInvoicesTotalAmount);
        Assert.NotNull(result.AttendanceAlert);
        Assert.Equal("NOT_RECORDED", result.AttendanceAlert.Status);

        var sprayOverdue = result.OverdueActivities.First(a => a.ActivityType == "SPRAY");
        Assert.Equal($"/sprays/{sprays[0].Id}", sprayOverdue.RouteUrl);

        var irrigationOverdue = result.OverdueActivities.First(a => a.ActivityType == "IRRIGATION");
        Assert.Equal($"/irrigations/{irrigations[0].Id}", irrigationOverdue.RouteUrl);

        var laborOverdue = result.OverdueActivities.First(a => a.ActivityType == "LABOR");
        Assert.Equal($"/activities/labor-activities/{laborActivities[0].Id}", laborOverdue.RouteUrl);

        var fertilizerToday = result.TodayActivities.First(a => a.ActivityType == "FERTILIZER");
        Assert.Equal($"/fertilizer-applications/{fertilizers[0].Id}", fertilizerToday.RouteUrl);
    }

    [Fact]
    public async Task GetPriorities_WhenActiveReiExists_IncludesReiSafetyAlert()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var farmId = Guid.NewGuid();

        var futureRei = now.AddHours(4);
        var sprays = new List<RawPrioritySpray>
        {
            new(Guid.NewGuid(), "Downy Mildew", farmId, "Farm A", "Block 2", null,
                today, null, now.AddHours(-2), "COMPLETED", futureRei)
        };

        _store.PrioritiesRawData = new DashboardPrioritiesRawData(
            sprays,
            new List<RawPriorityIrrigation>(),
            new List<RawPriorityFertilizer>(),
            new List<RawPriorityLaborActivity>(),
            new List<RawPriorityOverdueInvoice>(),
            new LaborDashboardRawData(new List<RawLaborAttendanceItem>(), new Dictionary<Guid, int>(), 0)
        );

        var result = await _service.GetPrioritiesAsync(_actor, farmId);

        Assert.NotNull(result);
        Assert.Single(result.ActiveReiAlerts);
        Assert.Equal("Downy Mildew", result.ActiveReiAlerts[0].TargetPest);
        Assert.True(result.ActiveReiAlerts[0].RemainingMinutes > 0);
        Assert.Equal($"/sprays/{sprays[0].Id}", result.ActiveReiAlerts[0].RouteUrl);
    }

    [Fact]
    public async Task GetUpcomingWork_ReturnsOrderedActivities()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var farmId = Guid.NewGuid();

        _store.UpcomingActivities = new List<RawUpcomingActivity>
        {
            new(Guid.NewGuid(), "SPRAY", "Spray Pest", farmId, "Farm A", null, null, today.AddDays(2), "10:00", "SCHEDULED", "Details A"),
            new(Guid.NewGuid(), "IRRIGATION", "Irrigation Plot", farmId, "Farm A", null, null, today.AddDays(1), "08:00", "SCHEDULED", "Details B")
        };

        var result = await _service.GetUpcomingWorkAsync(_actor, farmId, 7);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("IRRIGATION", result.Items[0].ActivityType);
        Assert.Equal($"/irrigations/{_store.UpcomingActivities[1].Id}", result.Items[0].RouteUrl);
        Assert.Equal("SPRAY", result.Items[1].ActivityType);
        Assert.Equal($"/sprays/{_store.UpcomingActivities[0].Id}", result.Items[1].RouteUrl);
    }

    [Fact]
    public async Task GetInventorySummary_ReturnsOutOfStockAndConsumption()
    {
        var farmId = Guid.NewGuid();

        var outOfStock = new List<RawOutOfStockItem>
        {
            new(Guid.NewGuid(), "Urea 46%", "SKU-UREA", "Fertilizer", Guid.NewGuid(), "Warehouse A", farmId, "Farm A", "kg", 0m)
        };

        var consumptions = new List<RawStockConsumption>
        {
            new(Guid.NewGuid(), "Chlorpyrifos", "Chemicals", 50m, "L")
        };

        _store.InventoryRawData = new DashboardInventoryRawData(outOfStock, consumptions, 25, 4);

        var result = await _service.GetInventorySummaryAsync(_actor, farmId);

        Assert.NotNull(result);
        Assert.Equal(1, result.OutOfStockCount);
        Assert.Single(result.OutOfStockItems);
        Assert.Single(result.RecentConsumptions);
        Assert.Equal(25, result.TotalItemsCount);
        Assert.Equal(4, result.TotalLocationsCount);
    }

    [Fact]
    public async Task GetFinancialSummary_PartitionsByCurrency()
    {
        var farmId = Guid.NewGuid();
        var inrId = Guid.NewGuid();
        var usdId = Guid.NewGuid();

        var directExpenses = new List<RawDirectExpenseItem>
        {
            new(inrId, "INR", "₹", 15000m),
            new(usdId, "USD", "$", 500m)
        };

        var invoices = new List<RawInvoiceFinancialItem>
        {
            new(inrId, "INR", "₹", 50000m, 20000m, true, true),
            new(usdId, "USD", "$", 1000m, 400m, false, true)
        };

        _store.FinancialRawData = new DashboardFinancialRawData(directExpenses, invoices);

        var result = await _service.GetFinancialSummaryAsync(_actor, farmId);

        Assert.NotNull(result);
        Assert.Equal(2, result.CurrencySummaries.Count);

        var inr = result.CurrencySummaries.First(c => c.CurrencyCode == "INR");
        Assert.Equal(15000m, inr.CurrentMonthDirectExpenses);
        Assert.Equal(50000m, inr.CurrentMonthInvoiced);
        Assert.Equal(20000m, inr.TotalOutstandingPayables);
        Assert.Equal(1, inr.OverdueInvoicesCount);

        var usd = result.CurrencySummaries.First(c => c.CurrencyCode == "USD");
        Assert.Equal(500m, usd.CurrentMonthDirectExpenses);
        Assert.Equal(1000m, usd.CurrentMonthInvoiced);
        Assert.Equal(400m, usd.TotalOutstandingPayables);
        Assert.Equal(0, usd.OverdueInvoicesCount);
    }

    [Fact]
    public async Task GetRecentActivity_ReturnsMappedEvents()
    {
        var farmId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _store.RecentActivities = new List<RawRecentActivity>
        {
            new(Guid.NewGuid(), "SPRAY_COMPLETED", "Spray Completed", "Treated 10 ac", now, farmId, "Farm A", "John")
        };

        var result = await _service.GetRecentActivityAsync(_actor, farmId, 15);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("SPRAY_COMPLETED", result.Items[0].EventType);
        Assert.Equal("Farm A", result.Items[0].FarmName);
    }

    private sealed class FakeDashboardStore : IDashboardStore
    {
        public IReadOnlyList<Farm> Farms { get; set; } = [];
        public int AreasCount { get; set; } = 0;
        public IReadOnlyList<CropPlantation> Plantations { get; set; } = [];
        public IReadOnlyList<CropCycle> Cycles { get; set; } = [];
        public IReadOnlyList<Unit> Units { get; set; } = [];
        public LaborDashboardRawData LaborData { get; set; } = new([], new Dictionary<Guid, int>(), 0);
        public DashboardPrioritiesRawData PrioritiesRawData { get; set; } = new([], [], [], [], [], new([], new Dictionary<Guid, int>(), 0));
        public IReadOnlyList<RawUpcomingActivity> UpcomingActivities { get; set; } = [];
        public DashboardInventoryRawData InventoryRawData { get; set; } = new([], [], 0, 0);
        public DashboardFinancialRawData FinancialRawData { get; set; } = new([], []);
        public IReadOnlyList<RawRecentActivity> RecentActivities { get; set; } = [];

        public Task<IReadOnlyList<Farm>> GetFarmsWithAreasAsync(Guid organizationId, Guid? farmId, CancellationToken cancellationToken = default)
            => Task.FromResult(Farms);

        public Task<int> GetActiveFarmAreasCountAsync(Guid organizationId, Guid? farmId, CancellationToken cancellationToken = default)
            => Task.FromResult(AreasCount);

        public Task<IReadOnlyList<CropPlantation>> GetPlantationsAsync(Guid organizationId, Guid? farmId, CancellationToken cancellationToken = default)
            => Task.FromResult(Plantations);

        public Task<IReadOnlyList<CropCycle>> GetCyclesAsync(Guid organizationId, Guid? farmId, CancellationToken cancellationToken = default)
            => Task.FromResult(Cycles);

        public Task<IReadOnlyList<Unit>> GetAreaUnitsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Units);

        public Task<LaborDashboardRawData> GetLaborDashboardDataAsync(Guid organizationId, Guid? farmId, DateOnly today, CancellationToken cancellationToken = default)
            => Task.FromResult(LaborData);

        public Task<DashboardPrioritiesRawData> GetPrioritiesRawDataAsync(Guid organizationId, Guid? farmId, DateOnly today, DateTimeOffset now, CancellationToken cancellationToken = default)
            => Task.FromResult(PrioritiesRawData);

        public Task<IReadOnlyList<RawUpcomingActivity>> GetUpcomingActivitiesAsync(Guid organizationId, Guid? farmId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
            => Task.FromResult(UpcomingActivities);

        public Task<DashboardInventoryRawData> GetInventoryRawDataAsync(Guid organizationId, Guid? farmId, DateOnly consumptionStartDate, CancellationToken cancellationToken = default)
            => Task.FromResult(InventoryRawData);

        public Task<DashboardFinancialRawData> GetFinancialRawDataAsync(Guid organizationId, Guid? farmId, DateOnly monthStartDate, DateOnly today, CancellationToken cancellationToken = default)
            => Task.FromResult(FinancialRawData);

        public Task<IReadOnlyList<RawRecentActivity>> GetRecentActivitiesAsync(Guid organizationId, Guid? farmId, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult(RecentActivities);
    }
}
