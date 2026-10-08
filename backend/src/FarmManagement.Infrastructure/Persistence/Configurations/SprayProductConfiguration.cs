using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SprayProductConfiguration : IEntityTypeConfiguration<SprayProduct>
{
    public void Configure(EntityTypeBuilder<SprayProduct> builder)
    {
        builder.ToTable("spray_products");

        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(sp => sp.SprayId).HasColumnName("spray_id").IsRequired();
        builder.HasOne(sp => sp.Spray)
            .WithMany(s => s.Products)
            .HasForeignKey(sp => sp.SprayId)
            .HasConstraintName("fk_spray_products_spray")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(sp => sp.InventoryItemId).HasColumnName("inventory_item_id").IsRequired();
        builder.HasOne(sp => sp.InventoryItem)
            .WithMany()
            .HasForeignKey(sp => sp.InventoryItemId)
            .HasConstraintName("fk_spray_products_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sp => sp.StorageLocationId).HasColumnName("storage_location_id");
        builder.HasOne(sp => sp.StorageLocation)
            .WithMany()
            .HasForeignKey(sp => sp.StorageLocationId)
            .HasConstraintName("fk_spray_products_storage_location")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sp => sp.PlannedQuantity).HasColumnName("planned_quantity").HasPrecision(18, 4);
        builder.Property(sp => sp.ActualQuantity).HasColumnName("actual_quantity").HasPrecision(18, 4);
        builder.Property(sp => sp.Dosage).HasColumnName("dosage").HasMaxLength(200);

        builder.Property(sp => sp.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(sp => sp.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(sp => sp.UpdatedAt).HasColumnName("updated_at");
        builder.Property(sp => sp.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(sp => new { sp.SprayId, sp.InventoryItemId })
            .HasDatabaseName("ux_spray_products_spray_item")
            .IsUnique();

        builder.HasIndex(sp => sp.InventoryItemId)
            .HasDatabaseName("ix_spray_products_inventory_item_id");

        builder.HasIndex(sp => sp.StorageLocationId)
            .HasDatabaseName("ix_spray_products_storage_location_id");
    }
}
