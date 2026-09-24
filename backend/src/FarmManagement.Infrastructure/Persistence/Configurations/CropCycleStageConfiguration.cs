using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropCycleStageConfiguration : IEntityTypeConfiguration<CropCycleStage>
{
    public void Configure(EntityTypeBuilder<CropCycleStage> builder)
    {
        builder.ToTable("crop_cycle_stages", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_crop_cycle_stages_status",
                "status IN ('NOT_STARTED', 'IN_PROGRESS', 'COMPLETED', 'SKIPPED', 'CANCELLED')");

            tableBuilder.HasCheckConstraint(
                "ck_crop_cycle_stages_sequence_number",
                "sequence_number > 0");

            tableBuilder.HasCheckConstraint(
                "ck_crop_cycle_stages_expected_duration_days",
                "expected_duration_days IS NULL OR expected_duration_days > 0");

            tableBuilder.HasCheckConstraint(
                "ck_crop_cycle_stages_planned_dates",
                "planned_end_date IS NULL OR planned_start_date IS NULL OR planned_end_date >= planned_start_date");

            tableBuilder.HasCheckConstraint(
                "ck_crop_cycle_stages_actual_dates",
                "actual_end_date IS NULL OR actual_start_date IS NULL OR actual_end_date >= actual_start_date");
        });

        builder.HasKey(stage => stage.Id);
        builder.Property(stage => stage.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(stage => stage.CropCycleId).HasColumnName("crop_cycle_id").IsRequired();
        builder.HasOne(stage => stage.CropCycle)
            .WithMany(cycle => cycle.Stages)
            .HasForeignKey(stage => stage.CropCycleId)
            .HasConstraintName("fk_crop_cycle_stage_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(stage => stage.LifecycleTemplateStageId).HasColumnName("lifecycle_template_stage_id").IsRequired();
        builder.HasOne(stage => stage.LifecycleTemplateStage)
            .WithMany()
            .HasForeignKey(stage => stage.LifecycleTemplateStageId)
            .HasConstraintName("fk_crop_cycle_stage_template_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(stage => stage.StageName).HasColumnName("stage_name").HasMaxLength(150).IsRequired();
        builder.Property(stage => stage.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(stage => stage.ExpectedDurationDays).HasColumnName("expected_duration_days");
        builder.Property(stage => stage.PlannedStartDate).HasColumnName("planned_start_date");
        builder.Property(stage => stage.PlannedEndDate).HasColumnName("planned_end_date");
        builder.Property(stage => stage.ActualStartDate).HasColumnName("actual_start_date");
        builder.Property(stage => stage.ActualEndDate).HasColumnName("actual_end_date");

        builder.Property(stage => stage.Status).HasColumnName("status")
            .HasConversion(
                status => ToStringValue(status),
                value => FromStringValue(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(stage => stage.Notes).HasColumnName("notes").HasColumnType("text");

        builder.Property(stage => stage.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(stage => stage.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(stage => stage.UpdatedAt).HasColumnName("updated_at");
        builder.Property(stage => stage.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(stage => new { stage.CropCycleId, stage.LifecycleTemplateStageId })
            .HasDatabaseName("ux_crop_cycle_stages_cycle_template_stage")
            .IsUnique();

        builder.HasIndex(stage => new { stage.CropCycleId, stage.SequenceNumber })
            .HasDatabaseName("ux_crop_cycle_stages_cycle_sequence")
            .IsUnique();

        builder.HasIndex(stage => stage.CropCycleId)
            .HasDatabaseName("ix_crop_cycle_stages_crop_cycle_id");

        builder.HasIndex(stage => stage.LifecycleTemplateStageId)
            .HasDatabaseName("ix_crop_cycle_stages_lifecycle_template_stage_id");

        builder.HasIndex(stage => new { stage.CropCycleId, stage.Status })
            .HasDatabaseName("ix_crop_cycle_stages_cycle_status");
    }

    private static string ToStringValue(CropCycleStageStatus status) => status switch
    {
        CropCycleStageStatus.NotStarted => "NOT_STARTED",
        CropCycleStageStatus.InProgress => "IN_PROGRESS",
        CropCycleStageStatus.Completed => "COMPLETED",
        CropCycleStageStatus.Skipped => "SKIPPED",
        CropCycleStageStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static CropCycleStageStatus FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "NOT_STARTED" or "NOTSTARTED" => CropCycleStageStatus.NotStarted,
        "IN_PROGRESS" or "INPROGRESS" => CropCycleStageStatus.InProgress,
        "COMPLETED" => CropCycleStageStatus.Completed,
        "SKIPPED" => CropCycleStageStatus.Skipped,
        "CANCELLED" or "CANCELED" => CropCycleStageStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
