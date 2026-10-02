using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
    {
        builder.ToTable("purchase_invoice_lines", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_purchase_invoice_lines_line_type", "line_type IN ('INVENTORY_ITEM', 'NON_INVENTORY_EXPENSE')");
            tableBuilder.HasCheckConstraint("ck_purchase_invoice_lines_amount", "line_amount >= 0 AND unit_price >= 0");
            tableBuilder.HasCheckConstraint("ck_purchase_invoice_lines_qty", "quantity IS NULL OR quantity > 0");
        });

        builder.HasKey(pil => pil.Id);

        builder.Property(pil => pil.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pil => pil.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(pil => pil.Organization)
            .WithMany()
            .HasForeignKey(pil => pil.OrganizationId)
            .HasConstraintName("fk_purchase_invoice_lines_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id")
            .IsRequired();

        builder.Property(pil => pil.LineType)
            .HasColumnName("line_type")
            .HasConversion(
                type => type == InvoiceLineType.InventoryItem ? "INVENTORY_ITEM" : "NON_INVENTORY_EXPENSE",
                value => value == "INVENTORY_ITEM" ? InvoiceLineType.InventoryItem : InvoiceLineType.NonInventoryExpense)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(pil => pil.InventoryItemId)
            .HasColumnName("inventory_item_id");

        builder.HasOne(pil => pil.InventoryItem)
            .WithMany()
            .HasForeignKey(pil => pil.InventoryItemId)
            .HasConstraintName("fk_purchase_invoice_lines_inventory_item")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.ExpenseCategoryId)
            .HasColumnName("expense_category_id");

        builder.HasOne(pil => pil.ExpenseCategory)
            .WithMany()
            .HasForeignKey(pil => pil.ExpenseCategoryId)
            .HasConstraintName("fk_purchase_invoice_lines_expense_category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pil => pil.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4);

        builder.Property(pil => pil.StockUnitId)
            .HasColumnName("stock_unit_id");

        builder.HasOne(pil => pil.StockUnit)
            .WithMany()
            .HasForeignKey(pil => pil.StockUnitId)
            .HasConstraintName("fk_purchase_invoice_lines_stock_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pil => pil.LineAmount)
            .HasColumnName("line_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pil => pil.FarmAreaId)
            .HasColumnName("farm_area_id");

        builder.HasOne(pil => pil.FarmArea)
            .WithMany()
            .HasForeignKey(pil => pil.FarmAreaId)
            .HasConstraintName("fk_purchase_invoice_lines_farm_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.PlantationId)
            .HasColumnName("plantation_id");

        builder.HasOne(pil => pil.Plantation)
            .WithMany()
            .HasForeignKey(pil => pil.PlantationId)
            .HasConstraintName("fk_purchase_invoice_lines_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.CropCycleId)
            .HasColumnName("crop_cycle_id");

        builder.HasOne(pil => pil.CropCycle)
            .WithMany()
            .HasForeignKey(pil => pil.CropCycleId)
            .HasConstraintName("fk_purchase_invoice_lines_crop_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pil => pil.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasIndex(pil => pil.PurchaseInvoiceId)
            .HasDatabaseName("ix_purchase_invoice_lines_invoice_id");

        builder.HasIndex(pil => pil.OrganizationId)
            .HasDatabaseName("ix_purchase_invoice_lines_organization_id");
    }
}
