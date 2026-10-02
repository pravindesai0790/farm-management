using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_expenses_amount", "amount > 0");
            tableBuilder.HasCheckConstraint("ck_expenses_status", "status IN ('DRAFT', 'POSTED', 'REVERSED')");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .HasConstraintName("fk_expenses_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(e => e.Farm)
            .WithMany()
            .HasForeignKey(e => e.FarmId)
            .HasConstraintName("fk_expenses_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ExpenseCategoryId)
            .HasColumnName("expense_category_id")
            .IsRequired();

        builder.HasOne(e => e.ExpenseCategory)
            .WithMany()
            .HasForeignKey(e => e.ExpenseCategoryId)
            .HasConstraintName("fk_expenses_expense_category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ExpenseDate)
            .HasColumnName("expense_date")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .HasConstraintName("fk_expenses_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.SupplierId)
            .HasColumnName("supplier_id");

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .HasConstraintName("fk_expenses_supplier")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(100);

        builder.Property(e => e.FarmAreaId)
            .HasColumnName("farm_area_id");

        builder.HasOne(e => e.FarmArea)
            .WithMany()
            .HasForeignKey(e => e.FarmAreaId)
            .HasConstraintName("fk_expenses_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.PlantationId)
            .HasColumnName("plantation_id");

        builder.HasOne(e => e.Plantation)
            .WithMany()
            .HasForeignKey(e => e.PlantationId)
            .HasConstraintName("fk_expenses_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CropCycleId)
            .HasColumnName("crop_cycle_id");

        builder.HasOne(e => e.CropCycle)
            .WithMany()
            .HasForeignKey(e => e.CropCycleId)
            .HasConstraintName("fk_expenses_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CropCycleStageId)
            .HasColumnName("crop_cycle_stage_id");

        builder.HasOne(e => e.CropCycleStage)
            .WithMany()
            .HasForeignKey(e => e.CropCycleStageId)
            .HasConstraintName("fk_expenses_crop_cycle_stage")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.AttachmentReference)
            .HasColumnName("attachment_reference")
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<ExpenseStatus>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.PostedAt)
            .HasColumnName("posted_at");

        builder.Property(e => e.PostedBy)
            .HasColumnName("posted_by");

        builder.Property(e => e.ReversedAt)
            .HasColumnName("reversed_at");

        builder.Property(e => e.ReversedBy)
            .HasColumnName("reversed_by");

        builder.Property(e => e.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(e => e.OrganizationId)
            .HasDatabaseName("ix_expenses_organization_id");

        builder.HasIndex(e => new { e.OrganizationId, e.FarmId, e.ExpenseDate })
            .HasDatabaseName("ix_expenses_org_farm_date");

        builder.HasIndex(e => new { e.OrganizationId, e.ExpenseCategoryId })
            .HasDatabaseName("ix_expenses_org_category");

        builder.HasIndex(e => new { e.OrganizationId, e.Status })
            .HasDatabaseName("ix_expenses_org_status");
    }
}
