using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public class ExpenseModelTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid CurrencyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateDraft_WithValidData_SetsStatusToDraft()
    {
        var expense = Expense.CreateDraft(
            OrgId,
            FarmId,
            CategoryId,
            new DateOnly(2026, 10, 1),
            "Tractor diesel fuel",
            450.75m,
            CurrencyId,
            UserId,
            referenceNumber: "INV-101",
            attachmentReference: "docs/fuel_receipt.pdf");

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal(OrgId, expense.OrganizationId);
        Assert.Equal(FarmId, expense.FarmId);
        Assert.Equal(CategoryId, expense.ExpenseCategoryId);
        Assert.Equal(new DateOnly(2026, 10, 1), expense.ExpenseDate);
        Assert.Equal("Tractor diesel fuel", expense.Description);
        Assert.Equal(450.75m, expense.Amount);
        Assert.Equal(CurrencyId, expense.CurrencyId);
        Assert.Equal("INV-101", expense.ReferenceNumber);
        Assert.Equal("docs/fuel_receipt.pdf", expense.AttachmentReference);
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Null(expense.PostedAt);
        Assert.Null(expense.ReversedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void CreateDraft_WithInvalidAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Expense.CreateDraft(
                OrgId,
                FarmId,
                CategoryId,
                new DateOnly(2026, 10, 1),
                "Description",
                amount,
                CurrencyId,
                UserId));
    }

    [Fact]
    public void UpdateDraft_WhenDraft_UpdatesProperties()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Old desc", 100m, CurrencyId, UserId);

        var newFarmId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var updaterId = Guid.NewGuid();

        expense.UpdateDraft(
            newFarmId,
            newCategoryId,
            new DateOnly(2026, 10, 2),
            "New desc",
            200m,
            CurrencyId,
            updaterId);

        Assert.Equal(newFarmId, expense.FarmId);
        Assert.Equal(newCategoryId, expense.ExpenseCategoryId);
        Assert.Equal("New desc", expense.Description);
        Assert.Equal(200m, expense.Amount);
        Assert.Equal(updaterId, expense.UpdatedBy);
    }

    [Fact]
    public void UpdateDraft_WhenPosted_ThrowsInvalidOperationException()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);
        expense.Post(UserId);

        Assert.Throws<InvalidOperationException>(() =>
            expense.UpdateDraft(
                FarmId, CategoryId, new DateOnly(2026, 10, 2), "Updated", 150m, CurrencyId, UserId));
    }

    [Fact]
    public void Post_WhenDraft_TransitionsToPosted()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);
        var postTime = DateTimeOffset.UtcNow;

        expense.Post(UserId, postTime);

        Assert.Equal(ExpenseStatus.Posted, expense.Status);
        Assert.Equal(UserId, expense.PostedBy);
        Assert.Equal(postTime, expense.PostedAt);
    }

    [Fact]
    public void Post_WhenAlreadyPosted_ThrowsInvalidOperationException()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);
        expense.Post(UserId);

        Assert.Throws<InvalidOperationException>(() => expense.Post(UserId));
    }

    [Fact]
    public void Reverse_WhenPosted_TransitionsToReversedWithReason()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);
        expense.Post(UserId);

        var reverserId = Guid.NewGuid();
        var reverseTime = DateTimeOffset.UtcNow;

        expense.Reverse("Duplicate entry recorded by mistake", reverserId, reverseTime);

        Assert.Equal(ExpenseStatus.Reversed, expense.Status);
        Assert.Equal("Duplicate entry recorded by mistake", expense.ReversalReason);
        Assert.Equal(reverserId, expense.ReversedBy);
        Assert.Equal(reverseTime, expense.ReversedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Reverse_WithoutReason_ThrowsArgumentException(string? invalidReason)
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);
        expense.Post(UserId);

        Assert.Throws<ArgumentException>(() => expense.Reverse(invalidReason!, UserId));
    }

    [Fact]
    public void Reverse_WhenDraft_ThrowsInvalidOperationException()
    {
        var expense = Expense.CreateDraft(
            OrgId, FarmId, CategoryId, new DateOnly(2026, 10, 1), "Fuel", 100m, CurrencyId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            expense.Reverse("Reason", UserId));
    }
}
