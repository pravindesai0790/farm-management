using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class PlantProtectionProductConfiguration : IEntityTypeConfiguration<PlantProtectionProduct>
{
    public void Configure(EntityTypeBuilder<PlantProtectionProduct> builder)
    {
        builder.ToTable("plant_protection_products");

        builder.HasKey(ppp => ppp.Id);
        builder.Property(ppp => ppp.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(ppp => ppp.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne(ppp => ppp.Organization)
            .WithMany()
            .HasForeignKey(ppp => ppp.OrganizationId)
            .HasConstraintName("fk_plant_protection_products_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ppp => ppp.InventoryItemId).HasColumnName("inventory_item_id").IsRequired();
        builder.HasOne(ppp => ppp.InventoryItem)
            .WithMany()
            .HasForeignKey(ppp => ppp.InventoryItemId)
            .HasConstraintName("fk_plant_protection_products_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ppp => ppp.ProductTypeId).HasColumnName("product_type_id").IsRequired();
        builder.HasOne(ppp => ppp.ProductType)
            .WithMany()
            .HasForeignKey(ppp => ppp.ProductTypeId)
            .HasConstraintName("fk_plant_protection_products_product_type")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ppp => ppp.ActiveIngredient).HasColumnName("active_ingredient").HasMaxLength(200);
        builder.Property(ppp => ppp.Manufacturer).HasColumnName("manufacturer").HasMaxLength(200);
        builder.Property(ppp => ppp.Description).HasColumnName("description");
        builder.Property(ppp => ppp.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(ppp => ppp.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(ppp => ppp.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(ppp => ppp.UpdatedAt).HasColumnName("updated_at");
        builder.Property(ppp => ppp.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(ppp => ppp.InventoryItemId)
            .HasDatabaseName("ux_plant_protection_products_inventory_item")
            .IsUnique();

        builder.HasIndex(ppp => ppp.ProductTypeId)
            .HasDatabaseName("ix_plant_protection_products_product_type_id");

        builder.HasIndex(ppp => ppp.OrganizationId)
            .HasDatabaseName("ix_plant_protection_products_organization_id");
    }
}
