using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class Expense
{
    private Expense()
    {
        Description = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid ExpenseCategoryId { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public string? AttachmentReference { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public ExpenseCategory? ExpenseCategory { get; private set; }
    public Currency? Currency { get; private set; }
    public Supplier? Supplier { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public CropCycle? CropCycle { get; private set; }
    public CropCycleStage? CropCycleStage { get; private set; }

    public static Expense CreateDraft(
        Guid organizationId,
        Guid farmId,
        Guid expenseCategoryId,
        DateOnly expenseDate,
        string description,
        decimal amount,
        Guid currencyId,
        Guid createdBy,
        Guid? supplierId = null,
        string? referenceNumber = null,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        string? attachmentReference = null)
    {
        ValidateCommon(organizationId, farmId, expenseCategoryId, description, amount, currencyId, createdBy);

        var now = DateTimeOffset.UtcNow;
        return new Expense
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FarmId = farmId,
            ExpenseCategoryId = expenseCategoryId,
            ExpenseDate = expenseDate,
            Description = description.Trim(),
            Amount = amount,
            CurrencyId = currencyId,
            SupplierId = supplierId,
            ReferenceNumber = NormalizeOptional(referenceNumber, 100),
            FarmAreaId = farmAreaId,
            PlantationId = plantationId,
            CropCycleId = cropCycleId,
            CropCycleStageId = cropCycleStageId,
            AttachmentReference = NormalizeOptional(attachmentReference, 500),
            Status = ExpenseStatus.Draft,
            CreatedAt = now,
            CreatedBy = createdBy
        };
    }

    public void UpdateDraft(
        Guid farmId,
        Guid expenseCategoryId,
        DateOnly expenseDate,
        string description,
        decimal amount,
        Guid currencyId,
        Guid updatedBy,
        Guid? supplierId = null,
        string? referenceNumber = null,
        Guid? farmAreaId = null,
        Guid? plantationId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        string? attachmentReference = null,
        DateTimeOffset? now = null)
    {
        if (Status != ExpenseStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft expenses can be modified. Current status: {Status}.");
        }

        ValidateCommon(OrganizationId, farmId, expenseCategoryId, description, amount, currencyId, updatedBy);

        FarmId = farmId;
        ExpenseCategoryId = expenseCategoryId;
        ExpenseDate = expenseDate;
        Description = description.Trim();
        Amount = amount;
        CurrencyId = currencyId;
        SupplierId = supplierId;
        ReferenceNumber = NormalizeOptional(referenceNumber, 100);
        FarmAreaId = farmAreaId;
        PlantationId = plantationId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        AttachmentReference = NormalizeOptional(attachmentReference, 500);
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Post(Guid postedBy, DateTimeOffset? now = null)
    {
        if (Status != ExpenseStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft expenses can be posted. Current status: {Status}.");
        }

        if (postedBy == Guid.Empty)
        {
            throw new ArgumentException("A posting user is required.", nameof(postedBy));
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        Status = ExpenseStatus.Posted;
        PostedAt = timestamp;
        PostedBy = postedBy;
        UpdatedAt = timestamp;
        UpdatedBy = postedBy;
    }

    public void Reverse(string reason, Guid reversedBy, DateTimeOffset? now = null)
    {
        if (Status != ExpenseStatus.Posted)
        {
            throw new InvalidOperationException($"Only posted expenses can be reversed. Current status: {Status}.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reversal reason is required.", nameof(reason));
        }

        if (reversedBy == Guid.Empty)
        {
            throw new ArgumentException("A reversing user is required.", nameof(reversedBy));
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        Status = ExpenseStatus.Reversed;
        ReversedAt = timestamp;
        ReversedBy = reversedBy;
        ReversalReason = reason.Trim();
        UpdatedAt = timestamp;
        UpdatedBy = reversedBy;
    }

    private static void ValidateCommon(
        Guid organizationId,
        Guid farmId,
        Guid expenseCategoryId,
        string description,
        decimal amount,
        Guid currencyId,
        Guid userId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("An organization is required.", nameof(organizationId));
        if (farmId == Guid.Empty) throw new ArgumentException("A farm is required.", nameof(farmId));
        if (expenseCategoryId == Guid.Empty) throw new ArgumentException("An expense category is required.", nameof(expenseCategoryId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("A description is required.", nameof(description));
        if (description.Trim().Length > 500) throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        if (currencyId == Guid.Empty) throw new ArgumentException("A currency is required.", nameof(currencyId));
        if (userId == Guid.Empty) throw new ArgumentException("A valid user identifier is required.", nameof(userId));
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
