using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .HasConstraintName("fk_expense_categories_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.IsSystemDefault)
            .HasColumnName("is_system_default")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(c => c.OrganizationId)
            .HasDatabaseName("ix_expense_categories_organization_id");

        builder.HasIndex(c => new { c.OrganizationId, c.Name })
            .HasDatabaseName("ix_expense_categories_organization_name");
    }
}
