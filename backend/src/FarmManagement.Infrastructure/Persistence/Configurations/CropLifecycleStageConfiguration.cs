using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropLifecycleStageConfiguration : IEntityTypeConfiguration<CropLifecycleStage>
{
    public void Configure(EntityTypeBuilder<CropLifecycleStage> builder)
    {
        builder.ToTable("crop_lifecycle_stages", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_crop_lifecycle_stages_sequence_number",
                "sequence_number > 0");

            tableBuilder.HasCheckConstraint(
                "ck_crop_lifecycle_stages_expected_duration_days",
                "expected_duration_days IS NULL OR expected_duration_days > 0");
        });

        builder.HasKey(stage => stage.Id);
        builder.Property(stage => stage.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(stage => stage.LifecycleTemplateId).HasColumnName("lifecycle_template_id").IsRequired();
        builder.Property(stage => stage.StageName).HasColumnName("stage_name").HasMaxLength(150).IsRequired();
        builder.Property(stage => stage.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(stage => stage.ExpectedDurationDays).HasColumnName("expected_duration_days");
        builder.Property(stage => stage.Description).HasColumnName("description");
        builder.Property(stage => stage.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.HasIndex(stage => new { stage.LifecycleTemplateId, stage.SequenceNumber })
            .HasDatabaseName("ux_lifecycle_stage_sequence")
            .IsUnique();
        builder.HasIndex(stage => stage.LifecycleTemplateId)
            .HasDatabaseName("ix_lifecycle_stages_template_id");
    }
}
