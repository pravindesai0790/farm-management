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
    }
}
