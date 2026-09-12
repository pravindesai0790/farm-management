using FarmManagement.Application.Common.Exceptions;
using FarmManagement.Application.Common.Models;
using FarmManagement.Application.DTOs.Labor;
using FarmManagement.Application.Interfaces.Labor;
using FarmManagement.Application.Services;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public sealed class LaborWageRateServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _currencyId = Guid.NewGuid();

    private LaborWageRateActor CreateActor() => new(_userId, _organizationId);

    private FakeLaborWageRateStore CreateStoreWithCurrency()
    {
        var store = new FakeLaborWageRateStore();
        store.Currencies.Add(new Currency("INR", "Indian Rupee", "₹", true, 1, _currencyId));
        return store;
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidOpenEndedRate_Succeeds()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var request = new CreateLaborWageRateRequest(
            Gender: "MALE",
            WageType: "FULL_DAY",
            WageRate: 450m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 1, 1),
            EffectiveTo: null,
            Notes: "Standard male full day rate");

        var response = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(response);
        Assert.Equal(450m, response.WageRate);
        Assert.Equal("MALE", response.Gender);
        Assert.Equal("FULL_DAY", response.WageType);
        Assert.Equal(new DateOnly(2026, 1, 1), response.EffectiveFrom);
        Assert.Null(response.EffectiveTo);
        Assert.True(response.IsActive);
        Assert.Single(store.AuditLogs);
        Assert.Equal("LaborWageRate.Created", store.AuditLogs[0].Action);
    }

    [Fact]
    public async Task CreateAsync_WhenWageRateIsZeroOrNegative_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var request = new CreateLaborWageRateRequest(
            Gender: "FEMALE",
            WageType: "FULL_DAY",
            WageRate: 0m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 1, 1));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenEffectiveToIsBeforeEffectiveFrom_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var request = new CreateLaborWageRateRequest(
            Gender: "FEMALE",
            WageType: "FULL_DAY",
            WageRate: 400m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 6, 1),
            EffectiveTo: new DateOnly(2026, 5, 31));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenCurrencyNotFoundOrInactive_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var request = new CreateLaborWageRateRequest(
            Gender: "FEMALE",
            WageType: "FULL_DAY",
            WageRate: 400m,
            CurrencyId: Guid.NewGuid(), // Non-existent
            EffectiveFrom: new DateOnly(2026, 1, 1));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenPrecedingOpenEndedRateExists_AutoClosesPrecedingRate()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        // Existing open-ended rate starting 2026-01-01
        var existingRate = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            450m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId,
            effectiveTo: null);
        store.Rates.Add(existingRate);

        // New rate effective 2026-07-01
        var request = new CreateLaborWageRateRequest(
            Gender: "MALE",
            WageType: "FULL_DAY",
            WageRate: 500m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 7, 1),
            EffectiveTo: null);

        var newRate = await service.CreateAsync(CreateActor(), request, "127.0.0.1");

        Assert.NotNull(newRate);
        Assert.Equal(500m, newRate.WageRate);
        Assert.Equal(new DateOnly(2026, 7, 1), newRate.EffectiveFrom);
        Assert.Null(newRate.EffectiveTo);

        // Verify previous rate was auto-closed to 2026-06-30
        Assert.Equal(new DateOnly(2026, 6, 30), existingRate.EffectiveTo);

        // Verify audit logs: Closed and Created
        Assert.Contains(store.AuditLogs, a => a.Action == "LaborWageRate.Closed");
        Assert.Contains(store.AuditLogs, a => a.Action == "LaborWageRate.Created");
    }

    [Fact]
    public async Task CreateAsync_WhenNewStartIsBeforeOrEqualExistingOpenRate_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var existingRate = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            450m,
            _currencyId,
            new DateOnly(2026, 6, 1),
            _userId,
            effectiveTo: null);
        store.Rates.Add(existingRate);

        // New rate with start date before existing start date
        var request = new CreateLaborWageRateRequest(
            Gender: "MALE",
            WageType: "FULL_DAY",
            WageRate: 500m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 1, 1),
            EffectiveTo: null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAsync_WhenOverlapsWithExistingClosedRate_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        // Rate from 2026-01-01 to 2026-06-30
        var existingRate = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            450m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId,
            effectiveTo: new DateOnly(2026, 6, 30));
        store.Rates.Add(existingRate);

        // New rate from 2026-05-01 to 2026-08-31 (overlaps with existing)
        var request = new CreateLaborWageRateRequest(
            Gender: "MALE",
            WageType: "FULL_DAY",
            WageRate: 480m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 5, 1),
            EffectiveTo: new DateOnly(2026, 8, 31));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateActor(), request, "127.0.0.1"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidChanges_Succeeds()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var existingRate = new LaborWageRate(
            _organizationId,
            Gender.Female,
            WageType.HalfDay,
            250m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId,
            effectiveTo: new DateOnly(2026, 6, 30),
            notes: "Initial");
        store.Rates.Add(existingRate);

        var request = new UpdateLaborWageRateRequest(
            WageRate: 275m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 1, 1),
            EffectiveTo: new DateOnly(2026, 6, 30),
            Notes: "Updated notes");

        var response = await service.UpdateAsync(CreateActor(), existingRate.Id, request, "127.0.0.1");

        Assert.Equal(275m, response.WageRate);
        Assert.Equal("Updated notes", response.Notes);
        Assert.Contains(store.AuditLogs, a => a.Action == "LaborWageRate.Updated");
    }

    [Fact]
    public async Task UpdateAsync_WhenOverlapsWithAnotherActiveRate_ThrowsValidationException()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var rate1 = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.Hourly,
            60m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId,
            effectiveTo: new DateOnly(2026, 6, 30));
        store.Rates.Add(rate1);

        var rate2 = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.Hourly,
            70m,
            _currencyId,
            new DateOnly(2026, 7, 1),
            _userId,
            effectiveTo: new DateOnly(2026, 12, 31));
        store.Rates.Add(rate2);

        // Try to update rate2 to start in May 2026 (overlapping rate1)
        var request = new UpdateLaborWageRateRequest(
            WageRate: 70m,
            CurrencyId: _currencyId,
            EffectiveFrom: new DateOnly(2026, 5, 1),
            EffectiveTo: new DateOnly(2026, 12, 31));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(CreateActor(), rate2.Id, request, "127.0.0.1"));
    }

    #endregion

    #region Activate / Deactivate Tests

    [Fact]
    public async Task DeactivateAsync_And_ActivateAsync_WorkCorrectly()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        var rate = new LaborWageRate(
            _organizationId,
            Gender.Other,
            WageType.Monthly,
            12000m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId);
        store.Rates.Add(rate);

        // Deactivate
        await service.DeactivateAsync(CreateActor(), rate.Id, "127.0.0.1");
        Assert.False(rate.IsActive);
        Assert.Contains(store.AuditLogs, a => a.Action == "LaborWageRate.Deactivated");

        // Reactivate
        await service.ActivateAsync(CreateActor(), rate.Id, "127.0.0.1");
        Assert.True(rate.IsActive);
        Assert.Contains(store.AuditLogs, a => a.Action == "LaborWageRate.Activated");
    }

    #endregion

    #region Lookup Applicable Rate Tests

    [Fact]
    public async Task LookupApplicableRateAsync_ResolvesCorrectPeriod()
    {
        var store = CreateStoreWithCurrency();
        var service = new LaborWageRateService(store);

        // Period 1: Jan to Jun ₹450
        var period1 = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            450m,
            _currencyId,
            new DateOnly(2026, 1, 1),
            _userId,
            effectiveTo: new DateOnly(2026, 6, 30));
        store.Rates.Add(period1);

        // Period 2: Jul onward ₹500
        var period2 = new LaborWageRate(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            500m,
            _currencyId,
            new DateOnly(2026, 7, 1),
            _userId,
            effectiveTo: null);
        store.Rates.Add(period2);

        // Query date in period 1: 2026-03-15
        var rate1Result = await service.LookupApplicableRateAsync(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            new DateOnly(2026, 3, 15));

        Assert.NotNull(rate1Result);
        Assert.Equal(450m, rate1Result.WageRate);

        // Query date in period 2: 2026-08-10
        var rate2Result = await service.LookupApplicableRateAsync(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            new DateOnly(2026, 8, 10));

        Assert.NotNull(rate2Result);
        Assert.Equal(500m, rate2Result.WageRate);

        // Query date before period 1: 2025-12-31 -> should be null
        var nullResult = await service.LookupApplicableRateAsync(
            _organizationId,
            Gender.Male,
            WageType.FullDay,
            new DateOnly(2025, 12, 31));

        Assert.Null(nullResult);
    }

    #endregion

    #region Fake Store

    private sealed class FakeLaborWageRateStore : ILaborWageRateStore
    {
        public List<LaborWageRate> Rates { get; } = [];
        public List<Currency> Currencies { get; } = [];
        public List<AuditLog> AuditLogs { get; } = [];

        public Task<PagedResponse<LaborWageRate>> ListAsync(
            Guid organizationId,
            int page,
            int pageSize,
            Gender? gender,
            WageType? wageType,
            bool? isActive,
            DateOnly? businessDate,
            CancellationToken cancellationToken = default)
        {
            var query = Rates.Where(r => r.OrganizationId == organizationId);
            if (gender.HasValue) query = query.Where(r => r.Gender == gender.Value);
            if (wageType.HasValue) query = query.Where(r => r.WageType == wageType.Value);
            if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
            if (businessDate.HasValue)
            {
                query = query.Where(r =>
                    r.EffectiveFrom <= businessDate.Value &&
                    (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= businessDate.Value));
            }

            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PagedResponse<LaborWageRate>(items, total, page, pageSize));
        }

        public Task<IReadOnlyList<LaborWageRate>> ListAllAsync(
            Guid organizationId,
            Gender? gender,
            WageType? wageType,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var query = Rates.Where(r => r.OrganizationId == organizationId);
            if (gender.HasValue) query = query.Where(r => r.Gender == gender.Value);
            if (wageType.HasValue) query = query.Where(r => r.WageType == wageType.Value);
            if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<LaborWageRate>>(query.ToList());
        }

        public Task<LaborWageRate?> FindAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
        {
            var rate = Rates.FirstOrDefault(r => r.Id == id && r.OrganizationId == organizationId);
            return Task.FromResult(rate);
        }

        public Task<IReadOnlyList<LaborWageRate>> GetActiveRatesForOrganizationAsync(
            Guid organizationId,
            Gender gender,
            WageType wageType,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var query = Rates.Where(r =>
                r.OrganizationId == organizationId &&
                r.Gender == gender &&
                r.WageType == wageType &&
                r.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(r => r.Id != excludeId.Value);
            }

            return Task.FromResult<IReadOnlyList<LaborWageRate>>(query.ToList());
        }

        public Task<LaborWageRate?> FindApplicableRateAsync(
            Guid organizationId,
            Gender gender,
            WageType wageType,
            DateOnly businessDate,
            CancellationToken cancellationToken = default)
        {
            var rate = Rates.Where(r =>
                r.OrganizationId == organizationId &&
                r.Gender == gender &&
                r.WageType == wageType &&
                r.IsActive &&
                r.EffectiveFrom <= businessDate &&
                (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= businessDate))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();

            return Task.FromResult(rate);
        }

        public Task<Currency?> FindCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Currencies.FirstOrDefault(c => c.Id == currencyId));
        }

        public void Add(LaborWageRate wageRate) => Rates.Add(wageRate);

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
