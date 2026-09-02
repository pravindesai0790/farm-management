using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class PlantationEndReasonConfiguration : IEntityTypeConfiguration<PlantationEndReason>
{
    public void Configure(EntityTypeBuilder<PlantationEndReason> builder)
    {
        builder.ToTable("plantation_end_reasons");
        builder.HasKey(reason => reason.Id);
        builder.Property(reason => reason.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(reason => reason.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(reason => reason.Organization).WithMany().HasForeignKey(reason => reason.OrganizationId)
            .HasConstraintName("fk_plantation_end_reason_organization").OnDelete(DeleteBehavior.Restrict);
        builder.Property(reason => reason.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(reason => reason.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(reason => reason.Description).HasColumnName("description");
        builder.Property(reason => reason.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(reason => reason.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(reason => reason.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(reason => reason.CreatedBy).HasColumnName("created_by");
        builder.Property(reason => reason.UpdatedAt).HasColumnName("updated_at");
        builder.Property(reason => reason.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(reason => new { reason.OrganizationId, reason.Code }).HasDatabaseName("ux_plantation_end_reasons_organization_code").IsUnique();
    }
}
