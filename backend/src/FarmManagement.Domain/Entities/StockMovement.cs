using FarmManagement.Domain.Enums;

namespace FarmManagement.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement()
    {
    }

    public StockMovement(
        Guid organizationId,
        StockMovementType movementType,
        Guid inventoryItemId,
        Guid farmId,
        Guid storageLocationId,
        decimal quantity,
        Guid stockUnitId,
        DateOnly movementDate,
        Guid createdBy,
        string? referenceNumber = null,
        string? notes = null,
        Guid? parentTransactionId = null,
        Guid? cropCycleId = null,
        Guid? cropCycleStageId = null,
        Guid? plantationId = null,
        Guid? farmAreaId = null,
        Guid? laborActivityId = null,
        Guid? reversedMovementId = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (!Enum.IsDefined(movementType))
        {
            throw new ArgumentOutOfRangeException(nameof(movementType), "Invalid stock movement type.");
        }

        if (inventoryItemId == Guid.Empty)
        {
            throw new ArgumentException("An inventory item is required.", nameof(inventoryItemId));
        }

        if (farmId == Guid.Empty)
        {
            throw new ArgumentException("A farm is required.", nameof(farmId));
        }

        if (storageLocationId == Guid.Empty)
        {
            throw new ArgumentException("A storage location is required.", nameof(storageLocationId));
        }

        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Movement quantity must be positive.");
        }

        if (stockUnitId == Guid.Empty)
        {
            throw new ArgumentException("A stock unit is required.", nameof(stockUnitId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        MovementType = movementType;
        InventoryItemId = inventoryItemId;
        FarmId = farmId;
        StorageLocationId = storageLocationId;
        Quantity = quantity;
        StockUnitId = stockUnitId;
        MovementDate = movementDate;
        ReferenceNumber = NormalizeOptional(referenceNumber);
        Notes = NormalizeOptional(notes);
        ParentTransactionId = parentTransactionId;
        CropCycleId = cropCycleId;
        CropCycleStageId = cropCycleStageId;
        PlantationId = plantationId;
        FarmAreaId = farmAreaId;
        LaborActivityId = laborActivityId;
        ReversedMovementId = reversedMovementId;
        IsReversed = false;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StorageLocationId { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid StockUnitId { get; private set; }
    public DateOnly MovementDate { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ParentTransactionId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? CropCycleStageId { get; private set; }
    public Guid? PlantationId { get; private set; }
    public Guid? FarmAreaId { get; private set; }
    public Guid? LaborActivityId { get; private set; }
    public bool IsReversed { get; private set; }
    public Guid? ReversalMovementId { get; private set; }
    public Guid? ReversedMovementId { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public Organization? Organization { get; private set; }
    public Farm? Farm { get; private set; }
    public StorageLocation? StorageLocation { get; private set; }
    public InventoryItem? InventoryItem { get; private set; }
    public Unit? StockUnit { get; private set; }
    public CropCycle? CropCycle { get; private set; }
    public CropCycleStage? CropCycleStage { get; private set; }
    public CropPlantation? Plantation { get; private set; }
    public FarmArea? FarmArea { get; private set; }
    public LaborActivity? LaborActivity { get; private set; }
    public StockMovement? ReversalMovement { get; private set; }
    public StockMovement? ReversedMovement { get; private set; }

    public void MarkAsReversed(Guid reversalMovementId, string reason)
    {
        if (IsReversed)
        {
            throw new InvalidOperationException("This movement has already been reversed.");
        }

        if (reversalMovementId == Guid.Empty)
        {
            throw new ArgumentException("A reversal movement identifier is required.", nameof(reversalMovementId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reversal reason is required.", nameof(reason));
        }

        IsReversed = true;
        ReversalMovementId = reversalMovementId;
        ReversalReason = reason.Trim();
    }

    public static bool IsReversalType(StockMovementType type) =>
        type is StockMovementType.OpeningStockReversal
             or StockMovementType.ReceiptReversal
             or StockMovementType.IssueReversal
             or StockMovementType.AdjustmentInReversal
             or StockMovementType.AdjustmentOutReversal
             or StockMovementType.TransferInReversal
             or StockMovementType.TransferOutReversal;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
