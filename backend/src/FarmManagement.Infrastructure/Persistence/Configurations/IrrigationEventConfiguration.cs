using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class IrrigationEventConfiguration : IEntityTypeConfiguration<IrrigationEvent>
{
    public void Configure(EntityTypeBuilder<IrrigationEvent> builder)
    {
        builder.ToTable("irrigation_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .HasConstraintName("fk_irrigation_events_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.FarmId).HasColumnName("farm_id").IsRequired();
        builder.HasOne(e => e.Farm)
            .WithMany()
            .HasForeignKey(e => e.FarmId)
            .HasConstraintName("fk_irrigation_events_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.FarmAreaId).HasColumnName("farm_area_id").IsRequired();
        builder.HasOne(e => e.FarmArea)
            .WithMany()
            .HasForeignKey(e => e.FarmAreaId)
            .HasConstraintName("fk_irrigation_events_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.PlantationId).HasColumnName("plantation_id");
        builder.HasOne(e => e.Plantation)
            .WithMany()
            .HasForeignKey(e => e.PlantationId)
            .HasConstraintName("fk_irrigation_events_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CropCycleId).HasColumnName("crop_cycle_id");
        builder.HasOne(e => e.CropCycle)
            .WithMany()
            .HasForeignKey(e => e.CropCycleId)
            .HasConstraintName("fk_irrigation_events_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CropCycleStageId).HasColumnName("crop_cycle_stage_id");
        builder.HasOne(e => e.CropCycleStage)
            .WithMany()
            .HasForeignKey(e => e.CropCycleStageId)
            .HasConstraintName("fk_irrigation_events_crop_cycle_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.IrrigationMethodId).HasColumnName("irrigation_method_id");
        builder.HasOne(e => e.IrrigationMethod)
            .WithMany()
            .HasForeignKey(e => e.IrrigationMethodId)
            .HasConstraintName("fk_irrigation_events_irrigation_method")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToStringValue(status),
                value => FromStringValue(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.PlannedAt).HasColumnName("planned_at");
        builder.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
        builder.Property(e => e.ActualStartedAt).HasColumnName("actual_started_at");
        builder.Property(e => e.ActualEndedAt).HasColumnName("actual_ended_at");
        builder.Property(e => e.ActualDurationMinutes).HasColumnName("actual_duration_minutes");

        builder.Property(e => e.PlannedWaterQuantity).HasColumnName("planned_water_quantity").HasPrecision(18, 4);
        builder.Property(e => e.PlannedWaterUnitId).HasColumnName("planned_water_unit_id");
        builder.HasOne(e => e.PlannedWaterUnit)
            .WithMany()
            .HasForeignKey(e => e.PlannedWaterUnitId)
            .HasConstraintName("fk_irrigation_events_planned_water_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ActualWaterQuantity).HasColumnName("actual_water_quantity").HasPrecision(18, 4);
        builder.Property(e => e.ActualWaterUnitId).HasColumnName("actual_water_unit_id");
        builder.HasOne(e => e.ActualWaterUnit)
            .WithMany()
            .HasForeignKey(e => e.ActualWaterUnitId)
            .HasConstraintName("fk_irrigation_events_actual_water_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(1000);
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_irrigation_events_organization_id");
        builder.HasIndex(e => e.FarmId).HasDatabaseName("ix_irrigation_events_farm_id");
        builder.HasIndex(e => e.FarmAreaId).HasDatabaseName("ix_irrigation_events_farm_area_id");
        builder.HasIndex(e => new { e.FarmId, e.Status }).HasDatabaseName("ix_irrigation_events_farm_status");
        builder.HasIndex(e => e.PlantationId).HasDatabaseName("ix_irrigation_events_plantation_id");
        builder.HasIndex(e => e.CropCycleId).HasDatabaseName("ix_irrigation_events_crop_cycle_id");
        builder.HasIndex(e => e.CropCycleStageId).HasDatabaseName("ix_irrigation_events_crop_cycle_stage_id");
        builder.HasIndex(e => e.ScheduledAt).HasDatabaseName("ix_irrigation_events_scheduled_at");
        builder.HasIndex(e => e.CompletedAt).HasDatabaseName("ix_irrigation_events_completed_at");
    }

    private static string ToStringValue(IrrigationStatus status) => status switch
    {
        IrrigationStatus.Draft => "DRAFT",
        IrrigationStatus.Scheduled => "SCHEDULED",
        IrrigationStatus.InProgress => "IN_PROGRESS",
        IrrigationStatus.Completed => "COMPLETED",
        IrrigationStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static IrrigationStatus FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "DRAFT" => IrrigationStatus.Draft,
        "SCHEDULED" => IrrigationStatus.Scheduled,
        "IN_PROGRESS" or "INPROGRESS" => IrrigationStatus.InProgress,
        "COMPLETED" => IrrigationStatus.Completed,
        "CANCELLED" or "CANCELED" => IrrigationStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
