using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class ApplicationMethodConfiguration : IEntityTypeConfiguration<ApplicationMethod>
{
    public void Configure(EntityTypeBuilder<ApplicationMethod> builder)
    {
        builder.ToTable("application_methods");

        builder.HasKey(am => am.Id);
        builder.Property(am => am.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(am => am.OrganizationId).HasColumnName("organization_id");
        builder.HasOne(am => am.Organization)
            .WithMany()
            .HasForeignKey(am => am.OrganizationId)
            .HasConstraintName("fk_application_methods_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(am => am.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(am => am.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(am => am.Description).HasColumnName("description");
        builder.Property(am => am.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0).IsRequired();
        builder.Property(am => am.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        builder.Property(am => am.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        builder.Property(am => am.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(am => am.CreatedBy).HasColumnName("created_by");
        builder.Property(am => am.UpdatedAt).HasColumnName("updated_at");
        builder.Property(am => am.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(am => new { am.OrganizationId, am.Code })
            .HasDatabaseName("ux_application_methods_organization_code")
            .IsUnique();

        builder.HasIndex(am => am.Code)
            .HasDatabaseName("ux_application_methods_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();
    }
}
