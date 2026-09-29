using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(item => item.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(item => item.Organization)
            .WithMany()
            .HasForeignKey(item => item.OrganizationId)
            .HasConstraintName("fk_inventory_items_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Sku)
            .HasColumnName("code_sku")
            .HasMaxLength(50);

        builder.Property(item => item.Description)
            .HasColumnName("description");

        builder.Property(item => item.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(item => item.StockUnitId)
            .HasColumnName("stock_unit_id")
            .IsRequired();

        builder.HasOne(item => item.StockUnit)
            .WithMany()
            .HasForeignKey(item => item.StockUnitId)
            .HasConstraintName("fk_inventory_items_stock_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(item => item.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(item => item.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(item => new { item.OrganizationId, item.Sku })
            .HasDatabaseName("ux_inventory_items_organization_sku")
            .HasFilter("code_sku IS NOT NULL")
            .IsUnique();

        builder.HasIndex(item => new { item.OrganizationId, item.Name })
            .HasDatabaseName("ix_inventory_items_organization_name");
    }
}
