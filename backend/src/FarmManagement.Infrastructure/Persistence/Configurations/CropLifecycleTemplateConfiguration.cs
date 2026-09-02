using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CropLifecycleTemplateConfiguration : IEntityTypeConfiguration<CropLifecycleTemplate>
{
    public void Configure(EntityTypeBuilder<CropLifecycleTemplate> builder)
    {
        builder.ToTable("crop_lifecycle_templates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(template => template.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(template => template.Organization).WithMany().HasForeignKey(template => template.OrganizationId)
            .HasConstraintName("fk_lifecycle_template_organization").OnDelete(DeleteBehavior.Restrict);

        builder.Property(template => template.CropId).HasColumnName("crop_id").IsRequired();
        builder.HasOne(template => template.Crop).WithMany().HasForeignKey(template => template.CropId)
            .HasConstraintName("fk_lifecycle_template_crop").OnDelete(DeleteBehavior.Restrict);

        builder.Property(template => template.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(template => template.Description).HasColumnName("description");
        builder.Property(template => template.IsDefault).HasColumnName("is_default").HasDefaultValue(false).IsRequired();
        builder.Property(template => template.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(template => template.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(template => template.CreatedBy).HasColumnName("created_by");
        builder.Property(template => template.UpdatedAt).HasColumnName("updated_at");
        builder.Property(template => template.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(template => new { template.CropId, template.OrganizationId })
            .HasDatabaseName("ix_lifecycle_templates_crop_organization");
        builder.HasIndex(template => template.OrganizationId)
            .HasDatabaseName("ix_lifecycle_templates_organization_id");
        builder.HasMany(template => template.Stages)
            .WithOne(stage => stage.LifecycleTemplate)
            .HasForeignKey(stage => stage.LifecycleTemplateId)
            .HasConstraintName("fk_lifecycle_stage_template")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
