using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FertilizerApplicationProductConfiguration : IEntityTypeConfiguration<FertilizerApplicationProduct>
{
    public void Configure(EntityTypeBuilder<FertilizerApplicationProduct> builder)
    {
        builder.ToTable("fertilizer_application_products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.FertilizerApplicationId)
            .HasColumnName("fertilizer_application_id")
            .IsRequired();

        builder.HasOne(p => p.FertilizerApplication)
            .WithMany(fa => fa.Products)
            .HasForeignKey(p => p.FertilizerApplicationId)
            .HasConstraintName("fk_fertilizer_application_products_fertilizer_application")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.InventoryItemId)
            .HasColumnName("inventory_item_id")
            .IsRequired();

        builder.HasOne(p => p.InventoryItem)
            .WithMany()
            .HasForeignKey(p => p.InventoryItemId)
            .HasConstraintName("fk_fertilizer_application_products_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.StorageLocationId)
            .HasColumnName("storage_location_id");

        builder.HasOne(p => p.StorageLocation)
            .WithMany()
            .HasForeignKey(p => p.StorageLocationId)
            .HasConstraintName("fk_fertilizer_application_products_storage_location")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.PlannedQuantity)
            .HasColumnName("planned_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.ActualQuantity)
            .HasColumnName("actual_quantity")
            .HasPrecision(18, 4);

        builder.Property(p => p.StockMovementId)
            .HasColumnName("stock_movement_id");

        builder.HasOne(p => p.StockMovement)
            .WithMany()
            .HasForeignKey(p => p.StockMovementId)
            .HasConstraintName("fk_fertilizer_application_products_stock_movement")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(p => new { p.FertilizerApplicationId, p.InventoryItemId })
            .HasDatabaseName("ux_fertilizer_application_products_application_item")
            .IsUnique();

        builder.HasIndex(p => p.InventoryItemId)
            .HasDatabaseName("ix_fertilizer_application_products_inventory_item_id");

        builder.HasIndex(p => p.StorageLocationId)
            .HasDatabaseName("ix_fertilizer_application_products_storage_location_id");

        builder.HasIndex(p => p.StockMovementId)
            .HasDatabaseName("ix_fertilizer_application_products_stock_movement_id");
    }
}
