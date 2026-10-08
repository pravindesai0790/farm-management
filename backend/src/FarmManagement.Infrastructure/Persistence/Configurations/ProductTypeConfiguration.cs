using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");

        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(pt => pt.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(pt => pt.Organization)
            .WithMany()
            .HasForeignKey(pt => pt.OrganizationId)
            .HasConstraintName("fk_product_types_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pt => pt.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(pt => pt.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(pt => pt.Description).HasColumnName("description");
        builder.Property(pt => pt.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0).IsRequired();
        builder.Property(pt => pt.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(pt => pt.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(pt => pt.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(pt => pt.CreatedBy).HasColumnName("created_by");
        builder.Property(pt => pt.UpdatedAt).HasColumnName("updated_at");
        builder.Property(pt => pt.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(pt => new { pt.OrganizationId, pt.Code })
            .HasDatabaseName("ux_product_types_organization_code")
            .IsUnique();

        builder.HasIndex(pt => pt.Code)
            .HasDatabaseName("ux_product_types_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();
    }
}
