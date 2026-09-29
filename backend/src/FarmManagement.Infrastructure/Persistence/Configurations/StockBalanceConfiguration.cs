using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("stock_balances", table =>
            table.HasCheckConstraint("chk_stock_balances_non_negative", "quantity_on_hand >= 0"));

        builder.HasKey(balance => balance.Id);

        builder.Property(balance => balance.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(balance => balance.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(balance => balance.Organization)
            .WithMany()
            .HasForeignKey(balance => balance.OrganizationId)
            .HasConstraintName("fk_stock_balances_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(balance => balance.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(balance => balance.Farm)
            .WithMany()
            .HasForeignKey(balance => balance.FarmId)
            .HasConstraintName("fk_stock_balances_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(balance => balance.StorageLocationId)
            .HasColumnName("storage_location_id")
            .IsRequired();

        builder.HasOne(balance => balance.StorageLocation)
            .WithMany()
            .HasForeignKey(balance => balance.StorageLocationId)
            .HasConstraintName("fk_stock_balances_storage_location")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(balance => balance.InventoryItemId)
            .HasColumnName("inventory_item_id")
            .IsRequired();

        builder.HasOne(balance => balance.InventoryItem)
            .WithMany()
            .HasForeignKey(balance => balance.InventoryItemId)
            .HasConstraintName("fk_stock_balances_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(balance => balance.QuantityOnHand)
            .HasColumnName("quantity_on_hand")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(balance => balance.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(balance => balance.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(balance => balance.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(balance => new { balance.StorageLocationId, balance.InventoryItemId })
            .HasDatabaseName("ux_stock_balances_location_item")
            .IsUnique();

        builder.HasIndex(balance => new { balance.OrganizationId, balance.FarmId })
            .HasDatabaseName("ix_stock_balances_organization_farm");

        builder.HasIndex(balance => new { balance.FarmId, balance.InventoryItemId })
            .HasDatabaseName("ix_stock_balances_farm_item");
    }
}
