using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemCategoryConfiguration : IEntityTypeConfiguration<InventoryItemCategory>
{
    public void Configure(EntityTypeBuilder<InventoryItemCategory> builder)
    {
        builder.ToTable("inventory_item_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .HasConstraintName("fk_inventory_item_categories_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.Examples)
            .HasColumnName("examples")
            .HasMaxLength(500);

        builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50)
            .HasDefaultValue("category")
            .IsRequired();

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(c => new { c.OrganizationId, c.Code })
            .HasDatabaseName("ux_inventory_item_categories_org_code")
            .IsUnique();

        builder.HasIndex(c => c.Code)
            .HasDatabaseName("ux_inventory_item_categories_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();
    }
}
