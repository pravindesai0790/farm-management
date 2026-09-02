using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropCycleConfiguration : IEntityTypeConfiguration<CropCycle>
{
    public void Configure(EntityTypeBuilder<CropCycle> builder)
    {
        builder.ToTable("crop_cycles");
        builder.HasKey(cycle => cycle.Id);
        builder.Property(cycle => cycle.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(cycle => cycle.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne(cycle => cycle.Organization).WithMany().HasForeignKey(cycle => cycle.OrganizationId)
            .HasConstraintName("fk_crop_cycle_organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(cycle => cycle.Plantation).WithMany().HasForeignKey(cycle => cycle.PlantationId)
            .HasConstraintName("fk_crop_cycle_plantation").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(cycle => cycle.CancellationReason).WithMany().HasForeignKey(cycle => cycle.CancellationReasonId)
            .HasConstraintName("fk_crop_cycle_cancellation_reason").OnDelete(DeleteBehavior.Restrict);
        builder.Property(cycle => cycle.PlantationId).HasColumnName("plantation_id").IsRequired();
        builder.Property(cycle => cycle.CycleCode).HasColumnName("cycle_code").HasMaxLength(50).IsRequired();
        builder.Property(cycle => cycle.CycleName).HasColumnName("cycle_name").HasMaxLength(200).IsRequired();
        builder.Property(cycle => cycle.SeasonYear).HasColumnName("season_year").IsRequired();
        builder.Property(cycle => cycle.SeasonName).HasColumnName("season_name").HasMaxLength(100);
        builder.Property(cycle => cycle.PlannedStartDate).HasColumnName("planned_start_date").IsRequired();
        builder.Property(cycle => cycle.ActualStartDate).HasColumnName("actual_start_date");
        builder.Property(cycle => cycle.ExpectedEndDate).HasColumnName("expected_end_date");
        builder.Property(cycle => cycle.ActualEndDate).HasColumnName("actual_end_date");
        builder.Property(cycle => cycle.Status).HasColumnName("status")
            .HasConversion(status => status.ToString().ToUpperInvariant(), value => Enum.Parse<CropCycleStatus>(value, true))
            .HasMaxLength(30).IsRequired();
        builder.Property(cycle => cycle.CancellationReasonId).HasColumnName("cancellation_reason_id");
        builder.Property(cycle => cycle.CancellationNotes).HasColumnName("cancellation_notes");
        builder.Property(cycle => cycle.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(cycle => cycle.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(cycle => cycle.UpdatedAt).HasColumnName("updated_at");
        builder.Property(cycle => cycle.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(cycle => new { cycle.OrganizationId, cycle.CycleCode })
            .HasDatabaseName("ux_crop_cycle_organization_code").IsUnique();
        builder.HasIndex(cycle => new { cycle.PlantationId, cycle.Status })
            .HasDatabaseName("ix_crop_cycles_plantation_status");
    }
}
