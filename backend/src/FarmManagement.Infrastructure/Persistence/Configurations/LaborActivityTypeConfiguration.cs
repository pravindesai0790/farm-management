using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborActivityTypeConfiguration : IEntityTypeConfiguration<LaborActivityType>
{
    public void Configure(EntityTypeBuilder<LaborActivityType> builder)
    {
        builder.ToTable("labor_activity_types", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_labor_activity_types_display_order",
                "display_order >= 0");

            tableBuilder.HasCheckConstraint(
                "ck_labor_activity_types_system_org",
                "(is_system = TRUE AND organization_id IS NULL) OR (is_system = FALSE AND organization_id IS NOT NULL)");
        });

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(type => type.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(type => type.Organization)
            .WithMany()
            .HasForeignKey(type => type.OrganizationId)
            .HasConstraintName("fk_labor_activity_type_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(type => type.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(type => type.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(type => type.Description)
            .HasColumnName("description");

        builder.Property(type => type.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(type => type.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(type => type.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(type => type.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(type => type.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(type => type.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(type => type.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(type => new { type.OrganizationId, type.Code })
            .HasDatabaseName("ux_labor_activity_types_organization_code")
            .IsUnique();

        builder.HasIndex(type => type.Code)
            .HasDatabaseName("ux_labor_activity_types_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();

        builder.HasIndex(type => type.OrganizationId)
            .HasDatabaseName("ix_labor_activity_types_organization_id");
    }
}
