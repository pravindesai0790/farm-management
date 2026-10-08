using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class TargetConfiguration : IEntityTypeConfiguration<Target>
{
    public void Configure(EntityTypeBuilder<Target> builder)
    {
        builder.ToTable("targets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(t => t.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(t => t.Organization)
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .HasConstraintName("fk_targets_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(150).IsRequired();

        builder.Property(t => t.TargetType)
            .HasColumnName("target_type")
            .HasConversion(
                type => type.ToString().ToUpperInvariant(),
                value => Enum.Parse<TargetType>(value, ignoreCase: true))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Description).HasColumnName("description");
        builder.Property(t => t.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0).IsRequired();
        builder.Property(t => t.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(t => new { t.OrganizationId, t.Code })
            .HasDatabaseName("ux_targets_organization_code")
            .IsUnique();

        builder.HasIndex(t => t.Code)
            .HasDatabaseName("ux_targets_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();

        builder.HasIndex(t => new { t.OrganizationId, t.TargetType })
            .HasDatabaseName("ix_targets_organization_type");
    }
}
