using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class IrrigationMethodConfiguration : IEntityTypeConfiguration<IrrigationMethod>
{
    public void Configure(EntityTypeBuilder<IrrigationMethod> builder)
    {
        builder.ToTable("irrigation_methods");

        builder.HasKey(im => im.Id);
        builder.Property(im => im.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(im => im.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(im => im.Organization)
            .WithMany()
            .HasForeignKey(im => im.OrganizationId)
            .HasConstraintName("fk_irrigation_methods_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(im => im.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(im => im.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(im => im.Description).HasColumnName("description");
        builder.Property(im => im.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0).IsRequired();
        builder.Property(im => im.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(im => im.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(im => im.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(im => im.CreatedBy).HasColumnName("created_by");
        builder.Property(im => im.UpdatedAt).HasColumnName("updated_at");
        builder.Property(im => im.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(im => new { im.OrganizationId, im.Code })
            .HasDatabaseName("ux_irrigation_methods_organization_code")
            .IsUnique();

        builder.HasIndex(im => im.Code)
            .HasDatabaseName("ux_irrigation_methods_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();
    }
}
