using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SprayConfiguration : IEntityTypeConfiguration<Spray>
{
    public void Configure(EntityTypeBuilder<Spray> builder)
    {
        builder.ToTable("sprays");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(s => s.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .HasConstraintName("fk_sprays_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.FarmId).HasColumnName("farm_id").IsRequired();
        builder.HasOne(s => s.Farm)
            .WithMany()
            .HasForeignKey(s => s.FarmId)
            .HasConstraintName("fk_sprays_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.FarmAreaId).HasColumnName("farm_area_id");
        builder.HasOne(s => s.FarmArea)
            .WithMany()
            .HasForeignKey(s => s.FarmAreaId)
            .HasConstraintName("fk_sprays_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.PlantationId).HasColumnName("plantation_id");
        builder.HasOne(s => s.Plantation)
            .WithMany()
            .HasForeignKey(s => s.PlantationId)
            .HasConstraintName("fk_sprays_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.CropCycleId).HasColumnName("crop_cycle_id");
        builder.HasOne(s => s.CropCycle)
            .WithMany()
            .HasForeignKey(s => s.CropCycleId)
            .HasConstraintName("fk_sprays_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.CropCycleStageId).HasColumnName("crop_cycle_stage_id");
        builder.HasOne(s => s.CropCycleStage)
            .WithMany()
            .HasForeignKey(s => s.CropCycleStageId)
            .HasConstraintName("fk_sprays_crop_cycle_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToStringValue(status),
                value => FromStringValue(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.PlannedDate).HasColumnName("planned_date");
        builder.Property(s => s.ScheduledDateTime).HasColumnName("scheduled_date_time");
        builder.Property(s => s.ActualApplicationDateTime).HasColumnName("actual_application_date_time");

        builder.Property(s => s.PlannedArea).HasColumnName("planned_area").HasPrecision(18, 4);
        builder.Property(s => s.PlannedAreaUnitId).HasColumnName("planned_area_unit_id");
        builder.HasOne(s => s.PlannedAreaUnit)
            .WithMany()
            .HasForeignKey(s => s.PlannedAreaUnitId)
            .HasConstraintName("fk_sprays_planned_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.ActualTreatedArea).HasColumnName("actual_treated_area").HasPrecision(18, 4);
        builder.Property(s => s.ActualTreatedAreaUnitId).HasColumnName("actual_treated_area_unit_id");
        builder.HasOne(s => s.ActualTreatedAreaUnit)
            .WithMany()
            .HasForeignKey(s => s.ActualTreatedAreaUnitId)
            .HasConstraintName("fk_sprays_actual_treated_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.WaterQuantity).HasColumnName("water_quantity").HasPrecision(18, 4);
        builder.Property(s => s.WaterUnitId).HasColumnName("water_unit_id");
        builder.HasOne(s => s.WaterUnit)
            .WithMany()
            .HasForeignKey(s => s.WaterUnitId)
            .HasConstraintName("fk_sprays_water_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.TargetId).HasColumnName("target_id");
        builder.HasOne(s => s.Target)
            .WithMany()
            .HasForeignKey(s => s.TargetId)
            .HasConstraintName("fk_sprays_target")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.ApplicationMethodId).HasColumnName("application_method_id");
        builder.HasOne(s => s.ApplicationMethod)
            .WithMany()
            .HasForeignKey(s => s.ApplicationMethodId)
            .HasConstraintName("fk_sprays_application_method")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.PurposeReason).HasColumnName("purpose_reason").HasMaxLength(1000);
        builder.Property(s => s.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");

        builder.HasMany(s => s.Products)
            .WithOne(p => p.Spray)
            .HasForeignKey(p => p.SprayId)
            .HasConstraintName("fk_spray_products_spray")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.OrganizationId).HasDatabaseName("ix_sprays_organization_id");
        builder.HasIndex(s => s.FarmId).HasDatabaseName("ix_sprays_farm_id");
        builder.HasIndex(s => new { s.FarmId, s.Status }).HasDatabaseName("ix_sprays_farm_status");
        builder.HasIndex(s => s.CropCycleId).HasDatabaseName("ix_sprays_crop_cycle_id");
        builder.HasIndex(s => s.ScheduledDateTime).HasDatabaseName("ix_sprays_scheduled_date_time");
    }

    private static string ToStringValue(SprayStatus status) => status switch
    {
        SprayStatus.Draft => "DRAFT",
        SprayStatus.Scheduled => "SCHEDULED",
        SprayStatus.InProgress => "IN_PROGRESS",
        SprayStatus.Completed => "COMPLETED",
        SprayStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static SprayStatus FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "DRAFT" => SprayStatus.Draft,
        "SCHEDULED" => SprayStatus.Scheduled,
        "IN_PROGRESS" or "INPROGRESS" => SprayStatus.InProgress,
        "COMPLETED" => SprayStatus.Completed,
        "CANCELLED" or "CANCELED" => SprayStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
