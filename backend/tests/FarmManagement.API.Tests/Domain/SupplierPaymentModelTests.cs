using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class SupplierPaymentModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsStatusCompleted()
    {
        var payment = SupplierPayment.Create(
            OrgId,
            SupplierId,
            new DateOnly(2026, 10, 2),
            1500.00m,
            CurrencyId,
            PaymentMethod.BankTransfer,
            UserId,
            referenceNumber: "TXN-999888",
            notes: "Bank transfer payment for fertilizer",
            idempotencyKey: "IDEMP-KEY-1");

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(OrgId, payment.OrganizationId);
        Assert.Equal(SupplierId, payment.SupplierId);
        Assert.Equal(new DateOnly(2026, 10, 2), payment.PaymentDate);
        Assert.Equal(1500.00m, payment.Amount);
        Assert.Equal(CurrencyId, payment.CurrencyId);
        Assert.Equal(PaymentMethod.BankTransfer, payment.PaymentMethod);
        Assert.Equal("TXN-999888", payment.ReferenceNumber);
        Assert.Equal("Bank transfer payment for fertilizer", payment.Notes);
        Assert.Equal(SupplierPaymentStatus.Completed, payment.Status);
        Assert.Equal("IDEMP-KEY-1", payment.IdempotencyKey);
        Assert.Empty(payment.Allocations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithInvalidAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SupplierPayment.Create(
                OrgId,
                SupplierId,
                new DateOnly(2026, 10, 2),
                amount,
                CurrencyId,
                PaymentMethod.Cash,
                UserId));
    }

    [Fact]
    public void AddAllocation_WhenCompleted_AddsAllocation()
    {
        var payment = SupplierPayment.Create(
            OrgId, SupplierId, new DateOnly(2026, 10, 2), 1000m, CurrencyId, PaymentMethod.BankTransfer, UserId);

        var allocation = SupplierPaymentAllocation.Create(
            OrgId, payment.Id, InvoiceId, 600m, UserId);

        payment.AddAllocation(allocation);

        Assert.Single(payment.Allocations);
        Assert.Equal(600m, payment.Allocations.First().AllocatedAmount);
    }

    [Fact]
    public void Reverse_WhenCompleted_TransitionsToReversedWithReason()
    {
        var payment = SupplierPayment.Create(
            OrgId, SupplierId, new DateOnly(2026, 10, 2), 1000m, CurrencyId, PaymentMethod.BankTransfer, UserId);

        var reverserId = Guid.NewGuid();
        var reverseTime = DateTimeOffset.UtcNow;

        payment.Reverse("Payment bounced by bank", reverserId, reverseTime);

        Assert.Equal(SupplierPaymentStatus.Reversed, payment.Status);
        Assert.Equal("Payment bounced by bank", payment.ReversalReason);
        Assert.Equal(reverserId, payment.ReversedBy);
        Assert.Equal(reverseTime, payment.ReversedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Reverse_WithoutReason_ThrowsArgumentException(string? invalidReason)
    {
        var payment = SupplierPayment.Create(
            OrgId, SupplierId, new DateOnly(2026, 10, 2), 1000m, CurrencyId, PaymentMethod.BankTransfer, UserId);

        Assert.Throws<ArgumentException>(() => payment.Reverse(invalidReason!, UserId));
    }

    [Fact]
    public void Reverse_WhenAlreadyReversed_ThrowsInvalidOperationException()
    {
        var payment = SupplierPayment.Create(
            OrgId, SupplierId, new DateOnly(2026, 10, 2), 1000m, CurrencyId, PaymentMethod.BankTransfer, UserId);
        payment.Reverse("Reason 1", UserId);

        Assert.Throws<InvalidOperationException>(() => payment.Reverse("Reason 2", UserId));
    }
}
