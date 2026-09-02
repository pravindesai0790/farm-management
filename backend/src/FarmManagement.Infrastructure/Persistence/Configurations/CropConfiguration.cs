using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropConfiguration : IEntityTypeConfiguration<Crop>
{
    public void Configure(EntityTypeBuilder<Crop> builder)
    {
        builder.ToTable("crops");
        builder.HasKey(crop => crop.Id);
        builder.Property(crop => crop.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(crop => crop.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(crop => crop.Organization).WithMany().HasForeignKey(crop => crop.OrganizationId)
            .HasConstraintName("fk_crops_organization").OnDelete(DeleteBehavior.Restrict);
        builder.Property(crop => crop.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(crop => crop.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(crop => crop.ScientificName).HasColumnName("scientific_name").HasMaxLength(200);
        builder.Property(crop => crop.CropType).HasColumnName("crop_type").HasMaxLength(50).IsRequired();
        builder.Property(crop => crop.CropDurationType).HasColumnName("crop_duration_type").HasMaxLength(30).IsRequired();
        builder.Property(crop => crop.Description).HasColumnName("description");
        builder.Property(crop => crop.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(crop => crop.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(crop => crop.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(crop => crop.CreatedBy).HasColumnName("created_by");
        builder.Property(crop => crop.UpdatedAt).HasColumnName("updated_at");
        builder.Property(crop => crop.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(crop => new { crop.OrganizationId, crop.Code }).HasDatabaseName("ux_crops_organization_code").IsUnique();
        builder.HasIndex(crop => crop.Code).HasDatabaseName("ux_crops_system_code").HasFilter("organization_id IS NULL").IsUnique();
        builder.HasIndex(crop => crop.OrganizationId).HasDatabaseName("ix_crops_organization_id");
    }
}
