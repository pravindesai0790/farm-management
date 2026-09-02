using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropLifecycleStageConfiguration : IEntityTypeConfiguration<CropLifecycleStage>
{
    public void Configure(EntityTypeBuilder<CropLifecycleStage> builder)
    {
        builder.ToTable("crop_lifecycle_stages");
        builder.HasKey(stage => stage.Id);
        builder.Property(stage => stage.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(stage => stage.LifecycleTemplateId).HasColumnName("lifecycle_template_id").IsRequired();
        builder.Property(stage => stage.StageCode).HasColumnName("stage_code").HasMaxLength(50).IsRequired();
        builder.Property(stage => stage.StageName).HasColumnName("stage_name").HasMaxLength(150).IsRequired();
        builder.Property(stage => stage.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(stage => stage.Description).HasColumnName("description");
        builder.Property(stage => stage.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.HasIndex(stage => new { stage.LifecycleTemplateId, stage.SequenceNumber })
            .HasDatabaseName("ux_lifecycle_stage_sequence")
            .IsUnique();
        builder.HasIndex(stage => stage.LifecycleTemplateId)
            .HasDatabaseName("ix_lifecycle_stages_template_id");
    }
}
