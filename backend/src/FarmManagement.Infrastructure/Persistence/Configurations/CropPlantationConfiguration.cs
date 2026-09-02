using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropPlantationConfiguration : IEntityTypeConfiguration<CropPlantation>
{
    public void Configure(EntityTypeBuilder<CropPlantation> builder)
    {
        builder.ToTable("crop_plantations");
        builder.HasKey(plantation => plantation.Id);
        builder.Property(plantation => plantation.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(plantation => plantation.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne(plantation => plantation.Organization).WithMany().HasForeignKey(plantation => plantation.OrganizationId)
            .HasConstraintName("fk_plantation_organization").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.FarmId).HasColumnName("farm_id").IsRequired();
        builder.HasOne(plantation => plantation.Farm).WithMany().HasForeignKey(plantation => plantation.FarmId)
            .HasConstraintName("fk_plantation_farm").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.FarmAreaId).HasColumnName("farm_area_id").IsRequired();
        builder.HasOne(plantation => plantation.FarmArea).WithMany().HasForeignKey(plantation => plantation.FarmAreaId)
            .HasConstraintName("fk_plantation_area").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.CropId).HasColumnName("crop_id").IsRequired();
        builder.HasOne(plantation => plantation.Crop).WithMany().HasForeignKey(plantation => plantation.CropId)
            .HasConstraintName("fk_plantation_crop").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.VarietyId).HasColumnName("variety_id");
        builder.HasOne(plantation => plantation.Variety).WithMany().HasForeignKey(plantation => plantation.VarietyId)
            .HasConstraintName("fk_plantation_variety").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.LifecycleTemplateId).HasColumnName("lifecycle_template_id");
        builder.HasOne(plantation => plantation.LifecycleTemplate).WithMany().HasForeignKey(plantation => plantation.LifecycleTemplateId)
            .HasConstraintName("fk_plantation_lifecycle").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.PlantationCode).HasColumnName("plantation_code").HasMaxLength(50).IsRequired();
        builder.Property(plantation => plantation.PlantationName).HasColumnName("plantation_name").HasMaxLength(200).IsRequired();
        builder.Property(plantation => plantation.AllocatedArea).HasColumnName("allocated_area").HasPrecision(18, 4).IsRequired();
        builder.Property(plantation => plantation.AreaUnitId).HasColumnName("area_unit_id").IsRequired();
        builder.HasOne(plantation => plantation.AreaUnit).WithMany().HasForeignKey(plantation => plantation.AreaUnitId)
            .HasConstraintName("fk_plantation_area_unit").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.PlantingDate).HasColumnName("planting_date").IsRequired();
        builder.Property(plantation => plantation.ExpectedEndDate).HasColumnName("expected_end_date");
        builder.Property(plantation => plantation.ActualEndDate).HasColumnName("actual_end_date");
        builder.Property(plantation => plantation.Status).HasColumnName("status")
            .HasConversion(status => status.ToString().ToUpperInvariant(), value => Enum.Parse<PlantationStatus>(value, true))
            .HasMaxLength(30).IsRequired();
        builder.Property(plantation => plantation.EndReasonId).HasColumnName("end_reason_id");
        builder.HasOne(plantation => plantation.EndReason).WithMany().HasForeignKey(plantation => plantation.EndReasonId)
            .HasConstraintName("fk_plantation_end_reason").OnDelete(DeleteBehavior.Restrict);
        builder.Property(plantation => plantation.EndNotes).HasColumnName("end_notes");
        builder.Property(plantation => plantation.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(plantation => plantation.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(plantation => plantation.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(plantation => plantation.UpdatedAt).HasColumnName("updated_at");
        builder.Property(plantation => plantation.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(plantation => new { plantation.OrganizationId, plantation.PlantationCode })
            .HasDatabaseName("ux_plantation_organization_code").IsUnique();
        builder.HasIndex(plantation => new { plantation.FarmAreaId, plantation.Status })
            .HasDatabaseName("ix_plantations_area_status");
    }
}
