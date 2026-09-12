using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class WorkerPaymentAllocationModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();
    private readonly Guid _earningId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateOnly _allocationDate = new(2026, 9, 15);

    [Fact]
    public void Create_WithValidParameters_ReturnsAllocation()
    {
        var allocation = WorkerPaymentAllocation.Create(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 450m,
            allocationType: PaymentAllocationType.EarningSettlement,
            allocationDate: _allocationDate,
            createdBy: _userId,
            notes: "Settling week 1");

        Assert.NotEqual(Guid.Empty, allocation.Id);
        Assert.Equal(_organizationId, allocation.OrganizationId);
        Assert.Equal(_paymentId, allocation.WorkerPaymentId);
        Assert.Equal(_earningId, allocation.WorkerEarningsLedgerId);
        Assert.Equal(450m, allocation.AllocatedAmount);
        Assert.Equal(PaymentAllocationType.EarningSettlement, allocation.AllocationType);
        Assert.Equal(_allocationDate, allocation.AllocationDate);
        Assert.Equal("Settling week 1", allocation.Notes);
        Assert.Equal(_userId, allocation.CreatedBy);
    }

    [Fact]
    public void CreateEarningSettlement_SetsCorrectAllocationType()
    {
        var allocation = WorkerPaymentAllocation.CreateEarningSettlement(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 500m,
            allocationDate: _allocationDate,
            createdBy: _userId);

        Assert.Equal(PaymentAllocationType.EarningSettlement, allocation.AllocationType);
    }

    [Fact]
    public void CreateAdvanceOffset_SetsCorrectAllocationType()
    {
        var allocation = WorkerPaymentAllocation.CreateAdvanceOffset(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 200m,
            allocationDate: _allocationDate,
            createdBy: _userId);

        Assert.Equal(PaymentAllocationType.AdvanceOffset, allocation.AllocationType);
    }

    [Fact]
    public void Create_WithEmptyOrganizationId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WorkerPaymentAllocation.Create(
                organizationId: Guid.Empty,
                workerPaymentId: _paymentId,
                workerEarningsLedgerId: _earningId,
                allocatedAmount: 100m,
                allocationType: PaymentAllocationType.EarningSettlement,
                allocationDate: _allocationDate,
                createdBy: _userId));

        Assert.Equal("organizationId", ex.ParamName);
    }

    [Fact]
    public void Create_WithEmptyPaymentId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WorkerPaymentAllocation.Create(
                organizationId: _organizationId,
                workerPaymentId: Guid.Empty,
                workerEarningsLedgerId: _earningId,
                allocatedAmount: 100m,
                allocationType: PaymentAllocationType.EarningSettlement,
                allocationDate: _allocationDate,
                createdBy: _userId));

        Assert.Equal("workerPaymentId", ex.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCreatedBy_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WorkerPaymentAllocation.Create(
                organizationId: _organizationId,
                workerPaymentId: _paymentId,
                workerEarningsLedgerId: _earningId,
                allocatedAmount: 100m,
                allocationType: PaymentAllocationType.EarningSettlement,
                allocationDate: _allocationDate,
                createdBy: Guid.Empty));

        Assert.Equal("createdBy", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithZeroOrNegativeAmount_ThrowsArgumentOutOfRangeException(decimal invalidAmount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkerPaymentAllocation.Create(
                organizationId: _organizationId,
                workerPaymentId: _paymentId,
                workerEarningsLedgerId: _earningId,
                allocatedAmount: invalidAmount,
                allocationType: PaymentAllocationType.EarningSettlement,
                allocationDate: _allocationDate,
                createdBy: _userId));

        Assert.Equal("allocatedAmount", ex.ParamName);
    }

    [Fact]
    public void ValidateOrganizationBoundary_ThrowsWhenPaymentOrganizationMismatch()
    {
        var allocation = WorkerPaymentAllocation.Create(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 100m,
            allocationType: PaymentAllocationType.EarningSettlement,
            allocationDate: _allocationDate,
            createdBy: _userId);

        var otherOrgPayment = WorkerPayment.CreatePayout(
            organizationId: Guid.NewGuid(),
            workerId: Guid.NewGuid(),
            paymentDate: _allocationDate,
            amount: 500m,
            currencyId: Guid.NewGuid(),
            paymentMethod: PaymentMethod.Cash,
            createdBy: _userId);

        Assert.Throws<InvalidOperationException>(() =>
            allocation.ValidateOrganizationBoundary(otherOrgPayment, null));
    }

    [Fact]
    public void ValidateOrganizationBoundary_ThrowsWhenEarningOrganizationMismatch()
    {
        var workerId = Guid.NewGuid();
        var allocation = WorkerPaymentAllocation.Create(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 100m,
            allocationType: PaymentAllocationType.EarningSettlement,
            allocationDate: _allocationDate,
            createdBy: _userId);

        var payment = WorkerPayment.CreatePayout(
            organizationId: _organizationId,
            workerId: workerId,
            paymentDate: _allocationDate,
            amount: 500m,
            currencyId: Guid.NewGuid(),
            paymentMethod: PaymentMethod.Cash,
            createdBy: _userId);

        var otherOrgEarning = WorkerEarningsLedger.CreateEarning(
            organizationId: Guid.NewGuid(),
            workerId: workerId,
            earningsDate: _allocationDate,
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 350m,
            currencyId: Guid.NewGuid(),
            createdBy: _userId);

        Assert.Throws<InvalidOperationException>(() =>
            allocation.ValidateOrganizationBoundary(payment, otherOrgEarning));
    }

    [Fact]
    public void ValidateOrganizationBoundary_ThrowsWhenWorkerMismatch()
    {
        var allocation = WorkerPaymentAllocation.Create(
            organizationId: _organizationId,
            workerPaymentId: _paymentId,
            workerEarningsLedgerId: _earningId,
            allocatedAmount: 100m,
            allocationType: PaymentAllocationType.EarningSettlement,
            allocationDate: _allocationDate,
            createdBy: _userId);

        var payment = WorkerPayment.CreatePayout(
            organizationId: _organizationId,
            workerId: Guid.NewGuid(),
            paymentDate: _allocationDate,
            amount: 500m,
            currencyId: Guid.NewGuid(),
            paymentMethod: PaymentMethod.Cash,
            createdBy: _userId);

        var otherWorkerEarning = WorkerEarningsLedger.CreateEarning(
            organizationId: _organizationId,
            workerId: Guid.NewGuid(),
            earningsDate: _allocationDate,
            wageType: WageType.FullDay,
            quantity: 1m,
            wageRate: 350m,
            currencyId: Guid.NewGuid(),
            createdBy: _userId);

        Assert.Throws<InvalidOperationException>(() =>
            allocation.ValidateOrganizationBoundary(payment, otherWorkerEarning));
    }
}
