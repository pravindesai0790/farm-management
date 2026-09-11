using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborCategoryConfiguration : IEntityTypeConfiguration<LaborCategory>
{
    public void Configure(EntityTypeBuilder<LaborCategory> builder)
    {
        builder.ToTable("labor_categories", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_labor_categories_system_org",
                "(is_system = TRUE AND organization_id IS NULL) OR (is_system = FALSE AND organization_id IS NOT NULL)");
        });

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(category => category.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(category => category.Organization)
            .WithMany()
            .HasForeignKey(category => category.OrganizationId)
            .HasConstraintName("fk_labor_categories_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasColumnName("description");

        builder.Property(category => category.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(category => category.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(category => category.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(category => category.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasMany(category => category.Workers)
            .WithOne(worker => worker.LaborCategory)
            .HasForeignKey(worker => worker.LaborCategoryId)
            .HasConstraintName("fk_workers_labor_category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => new { category.OrganizationId, category.Name })
            .HasDatabaseName("ux_labor_categories_organization_name")
            .IsUnique();

        builder.HasIndex(category => category.Name)
            .HasDatabaseName("ux_labor_categories_system_name")
            .HasFilter("organization_id IS NULL")
            .IsUnique();

        builder.HasIndex(category => category.OrganizationId)
            .HasDatabaseName("ix_labor_categories_organization_id");
    }
}
