using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropVarietyConfiguration : IEntityTypeConfiguration<CropVariety>
{
    public void Configure(EntityTypeBuilder<CropVariety> builder)
    {
        builder.ToTable("crop_varieties");
        builder.HasKey(variety => variety.Id);
        builder.Property(variety => variety.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(variety => variety.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(variety => variety.Organization).WithMany().HasForeignKey(variety => variety.OrganizationId)
            .HasConstraintName("fk_crop_varieties_organization").OnDelete(DeleteBehavior.Restrict);
        builder.Property(variety => variety.CropId).HasColumnName("crop_id").IsRequired();
        builder.HasOne(variety => variety.Crop).WithMany().HasForeignKey(variety => variety.CropId)
            .HasConstraintName("fk_crop_variety_crop").OnDelete(DeleteBehavior.Restrict);
        builder.Property(variety => variety.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(variety => variety.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(variety => variety.Description).HasColumnName("description");
        builder.Property(variety => variety.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(variety => variety.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(variety => variety.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(variety => variety.CreatedBy).HasColumnName("created_by");
        builder.Property(variety => variety.UpdatedAt).HasColumnName("updated_at");
        builder.Property(variety => variety.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(variety => new { variety.CropId, variety.OrganizationId, variety.Code })
            .HasDatabaseName("ux_crop_varieties_organization_code").IsUnique();
        builder.HasIndex(variety => new { variety.CropId, variety.Code })
            .HasDatabaseName("ux_crop_varieties_system_code").HasFilter("organization_id IS NULL").IsUnique();
        builder.HasIndex(variety => variety.OrganizationId).HasDatabaseName("ix_crop_varieties_organization_id");
    }
}
