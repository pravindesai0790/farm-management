using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FertilizerApplicationConfiguration : IEntityTypeConfiguration<FertilizerApplication>
{
    public void Configure(EntityTypeBuilder<FertilizerApplication> builder)
    {
        builder.ToTable("fertilizer_applications");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(f => f.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(f => f.Organization)
            .WithMany()
            .HasForeignKey(f => f.OrganizationId)
            .HasConstraintName("fk_fertilizer_applications_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion(
                status => ToStringValue(status),
                value => FromStringValue(value))
            .IsRequired();

        builder.Property(f => f.FarmId)
            .HasColumnName("farm_id");

        builder.HasOne(f => f.Farm)
            .WithMany()
            .HasForeignKey(f => f.FarmId)
            .HasConstraintName("fk_fertilizer_applications_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.FarmAreaId)
            .HasColumnName("farm_area_id");

        builder.HasOne(f => f.FarmArea)
            .WithMany()
            .HasForeignKey(f => f.FarmAreaId)
            .HasConstraintName("fk_fertilizer_applications_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.PlantationId)
            .HasColumnName("plantation_id");

        builder.HasOne(f => f.Plantation)
            .WithMany()
            .HasForeignKey(f => f.PlantationId)
            .HasConstraintName("fk_fertilizer_applications_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.CropCycleId)
            .HasColumnName("crop_cycle_id");

        builder.HasOne(f => f.CropCycle)
            .WithMany()
            .HasForeignKey(f => f.CropCycleId)
            .HasConstraintName("fk_fertilizer_applications_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.CropCycleStageId)
            .HasColumnName("crop_cycle_stage_id");

        builder.HasOne(f => f.CropCycleStage)
            .WithMany()
            .HasForeignKey(f => f.CropCycleStageId)
            .HasConstraintName("fk_fertilizer_applications_crop_cycle_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.ApplicationMethodId)
            .HasColumnName("application_method_id");

        builder.HasOne(f => f.ApplicationMethod)
            .WithMany()
            .HasForeignKey(f => f.ApplicationMethodId)
            .HasConstraintName("fk_fertilizer_applications_application_method")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.OtherMethodDescription)
            .HasColumnName("other_method_description")
            .HasMaxLength(500);

        builder.Property(f => f.IrrigationEventId)
            .HasColumnName("irrigation_event_id");

        builder.HasOne(f => f.IrrigationEvent)
            .WithMany()
            .HasForeignKey(f => f.IrrigationEventId)
            .HasConstraintName("fk_fertilizer_applications_irrigation_event")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.PurposeReason)
            .HasColumnName("purpose_reason")
            .HasMaxLength(500);

        builder.Property(f => f.PlannedDate)
            .HasColumnName("planned_date");

        builder.Property(f => f.ScheduledDate)
            .HasColumnName("scheduled_date");

        builder.Property(f => f.ActualApplicationDate)
            .HasColumnName("actual_application_date");

        builder.Property(f => f.PlannedArea)
            .HasColumnName("planned_area")
            .HasPrecision(18, 4);

        builder.Property(f => f.PlannedAreaUnitId)
            .HasColumnName("planned_area_unit_id");

        builder.HasOne(f => f.PlannedAreaUnit)
            .WithMany()
            .HasForeignKey(f => f.PlannedAreaUnitId)
            .HasConstraintName("fk_fertilizer_applications_planned_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.ActualArea)
            .HasColumnName("actual_area")
            .HasPrecision(18, 4);

        builder.Property(f => f.ActualAreaUnitId)
            .HasColumnName("actual_area_unit_id");

        builder.HasOne(f => f.ActualAreaUnit)
            .WithMany()
            .HasForeignKey(f => f.ActualAreaUnitId)
            .HasConstraintName("fk_fertilizer_applications_actual_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(f => f.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(f => f.CancelledBy)
            .HasColumnName("cancelled_by");

        builder.Property(f => f.StartedAt)
            .HasColumnName("started_at");

        builder.Property(f => f.StartedBy)
            .HasColumnName("started_by");

        builder.Property(f => f.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(f => f.CompletedBy)
            .HasColumnName("completed_by");

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(f => f.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(f => f.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(f => f.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(1u)
            .IsRequired();

        builder.Property(f => f.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        builder.HasMany(f => f.Products)
            .WithOne(p => p.FertilizerApplication)
            .HasForeignKey(p => p.FertilizerApplicationId)
            .HasConstraintName("fk_fertilizer_app_products_app")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(f => new { f.OrganizationId, f.ReferenceNumber })
            .HasDatabaseName("ux_fertilizer_applications_org_ref")
            .IsUnique();

        builder.HasIndex(f => new { f.OrganizationId, f.Status })
            .HasDatabaseName("ix_fertilizer_applications_org_status");

        builder.HasIndex(f => f.FarmId)
            .HasDatabaseName("ix_fertilizer_applications_farm_id");

        builder.HasIndex(f => f.FarmAreaId)
            .HasDatabaseName("ix_fertilizer_applications_farm_area_id");

        builder.HasIndex(f => f.PlantationId)
            .HasDatabaseName("ix_fertilizer_applications_plantation_id");

        builder.HasIndex(f => f.CropCycleId)
            .HasDatabaseName("ix_fertilizer_applications_crop_cycle_id");

        builder.HasIndex(f => f.CropCycleStageId)
            .HasDatabaseName("ix_fertilizer_applications_crop_cycle_stage_id");

        builder.HasIndex(f => f.PlannedDate)
            .HasDatabaseName("ix_fertilizer_applications_planned_date");

        builder.HasIndex(f => f.ScheduledDate)
            .HasDatabaseName("ix_fertilizer_applications_scheduled_date");

        builder.HasIndex(f => f.ActualApplicationDate)
            .HasDatabaseName("ix_fertilizer_applications_actual_app_date");

        builder.HasIndex(f => f.CreatedAt)
            .HasDatabaseName("ix_fertilizer_applications_created_at");

        builder.HasIndex(f => new { f.OrganizationId, f.IdempotencyKey })
            .HasDatabaseName("ix_fertilizer_applications_org_idempotency");
    }

    private static string ToStringValue(FertilizerApplicationStatus status) => status switch
    {
        FertilizerApplicationStatus.Draft => "DRAFT",
        FertilizerApplicationStatus.Planned => "PLANNED",
        FertilizerApplicationStatus.Scheduled => "SCHEDULED",
        FertilizerApplicationStatus.InProgress => "IN_PROGRESS",
        FertilizerApplicationStatus.Completed => "COMPLETED",
        FertilizerApplicationStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static FertilizerApplicationStatus FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "DRAFT" => FertilizerApplicationStatus.Draft,
        "PLANNED" => FertilizerApplicationStatus.Planned,
        "SCHEDULED" => FertilizerApplicationStatus.Scheduled,
        "IN_PROGRESS" or "INPROGRESS" => FertilizerApplicationStatus.InProgress,
        "COMPLETED" => FertilizerApplicationStatus.Completed,
        "CANCELLED" or "CANCELED" => FertilizerApplicationStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
