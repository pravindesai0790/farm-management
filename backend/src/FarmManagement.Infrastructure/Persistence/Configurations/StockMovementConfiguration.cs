using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(movement => movement.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(movement => movement.Organization)
            .WithMany()
            .HasForeignKey(movement => movement.OrganizationId)
            .HasConstraintName("fk_stock_movements_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.MovementType)
            .HasColumnName("movement_type")
            .HasConversion(
                type => type.ToString().ToUpperInvariant(),
                value => Enum.Parse<StockMovementType>(value, ignoreCase: true))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(movement => movement.InventoryItemId)
            .HasColumnName("inventory_item_id")
            .IsRequired();

        builder.HasOne(movement => movement.InventoryItem)
            .WithMany()
            .HasForeignKey(movement => movement.InventoryItemId)
            .HasConstraintName("fk_stock_movements_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(movement => movement.Farm)
            .WithMany()
            .HasForeignKey(movement => movement.FarmId)
            .HasConstraintName("fk_stock_movements_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.StorageLocationId)
            .HasColumnName("storage_location_id")
            .IsRequired();

        builder.HasOne(movement => movement.StorageLocation)
            .WithMany()
            .HasForeignKey(movement => movement.StorageLocationId)
            .HasConstraintName("fk_stock_movements_storage_location")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(movement => movement.StockUnitId)
            .HasColumnName("stock_unit_id")
            .IsRequired();

        builder.HasOne(movement => movement.StockUnit)
            .WithMany()
            .HasForeignKey(movement => movement.StockUnitId)
            .HasConstraintName("fk_stock_movements_stock_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.MovementDate)
            .HasColumnName("movement_date")
            .IsRequired();

        builder.Property(movement => movement.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(100);

        builder.Property(movement => movement.Notes)
            .HasColumnName("notes");

        builder.Property(movement => movement.ParentTransactionId)
            .HasColumnName("parent_transaction_id");

        builder.Property(movement => movement.CropCycleId)
            .HasColumnName("crop_cycle_id");

        builder.HasOne(movement => movement.CropCycle)
            .WithMany()
            .HasForeignKey(movement => movement.CropCycleId)
            .HasConstraintName("fk_stock_movements_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.CropCycleStageId)
            .HasColumnName("crop_cycle_stage_id");

        builder.HasOne(movement => movement.CropCycleStage)
            .WithMany()
            .HasForeignKey(movement => movement.CropCycleStageId)
            .HasConstraintName("fk_stock_movements_crop_cycle_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.PlantationId)
            .HasColumnName("plantation_id");

        builder.HasOne(movement => movement.Plantation)
            .WithMany()
            .HasForeignKey(movement => movement.PlantationId)
            .HasConstraintName("fk_stock_movements_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.FarmAreaId)
            .HasColumnName("farm_area_id");

        builder.HasOne(movement => movement.FarmArea)
            .WithMany()
            .HasForeignKey(movement => movement.FarmAreaId)
            .HasConstraintName("fk_stock_movements_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.LaborActivityId)
            .HasColumnName("labor_activity_id");

        builder.HasOne(movement => movement.LaborActivity)
            .WithMany()
            .HasForeignKey(movement => movement.LaborActivityId)
            .HasConstraintName("fk_stock_movements_labor_activity")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(movement => movement.IsReversed)
            .HasColumnName("is_reversed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(movement => movement.ReversalMovementId)
            .HasColumnName("reversal_movement_id");

        builder.HasOne(movement => movement.ReversalMovement)
            .WithMany()
            .HasForeignKey(movement => movement.ReversalMovementId)
            .HasConstraintName("fk_stock_movements_reversal_movement")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.ReversedMovementId)
            .HasColumnName("reversed_movement_id");

        builder.HasOne(movement => movement.ReversedMovement)
            .WithMany()
            .HasForeignKey(movement => movement.ReversedMovementId)
            .HasConstraintName("fk_stock_movements_reversed_movement")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(movement => movement.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasMaxLength(1000);

        builder.Property(movement => movement.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(movement => movement.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasIndex(movement => new { movement.OrganizationId, movement.MovementDate })
            .HasDatabaseName("ix_stock_movements_organization_date");

        builder.HasIndex(movement => new { movement.FarmId, movement.StorageLocationId, movement.MovementDate })
            .HasDatabaseName("ix_stock_movements_farm_location_date");

        builder.HasIndex(movement => new { movement.InventoryItemId, movement.MovementDate })
            .HasDatabaseName("ix_stock_movements_item_date");

        builder.HasIndex(movement => movement.ParentTransactionId)
            .HasDatabaseName("ix_stock_movements_parent_transaction")
            .HasFilter("parent_transaction_id IS NOT NULL");

        builder.HasIndex(movement => movement.CropCycleId)
            .HasDatabaseName("ix_stock_movements_crop_cycle")
            .HasFilter("crop_cycle_id IS NOT NULL");

        builder.HasIndex(movement => movement.CropCycleStageId)
            .HasDatabaseName("ix_stock_movements_crop_cycle_stage")
            .HasFilter("crop_cycle_stage_id IS NOT NULL");

        builder.HasIndex(movement => movement.PlantationId)
            .HasDatabaseName("ix_stock_movements_plantation")
            .HasFilter("plantation_id IS NOT NULL");

        builder.HasIndex(movement => movement.FarmAreaId)
            .HasDatabaseName("ix_stock_movements_farm_area")
            .HasFilter("farm_area_id IS NOT NULL");

        builder.HasIndex(movement => movement.LaborActivityId)
            .HasDatabaseName("ix_stock_movements_labor_activity")
            .HasFilter("labor_activity_id IS NOT NULL");

        builder.HasIndex(movement => movement.ReversalMovementId)
            .HasDatabaseName("ix_stock_movements_reversal_movement")
            .HasFilter("reversal_movement_id IS NOT NULL");

        builder.HasIndex(movement => movement.ReversedMovementId)
            .HasDatabaseName("ix_stock_movements_reversed_movement")
            .HasFilter("reversed_movement_id IS NOT NULL");

        builder.HasIndex(movement => new { movement.OrganizationId, movement.IsReversed })
            .HasDatabaseName("ix_stock_movements_organization_is_reversed");

        // Enforce database-level uniqueness for active (non-reversed) opening stock per storage location and inventory item
        builder.HasIndex(movement => new { movement.StorageLocationId, movement.InventoryItemId })
            .HasDatabaseName("ux_stock_movements_opening_stock_location_item")
            .HasFilter("movement_type = 'OPENINGSTOCK' AND is_reversed = FALSE")
            .IsUnique();
    }
}
