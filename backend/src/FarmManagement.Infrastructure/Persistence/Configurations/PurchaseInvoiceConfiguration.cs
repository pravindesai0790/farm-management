using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("purchase_invoices", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_purchase_invoices_status", "status IN ('DRAFT', 'POSTED', 'REVERSED')");
            tableBuilder.HasCheckConstraint(
                "ck_purchase_invoices_financials",
                "subtotal >= 0 AND total_amount >= 0 AND tax_amount >= 0 AND other_charges >= 0 AND discount_amount >= 0");
            tableBuilder.HasCheckConstraint(
                "ck_purchase_invoices_due_date",
                "due_date IS NULL OR due_date >= invoice_date");
        });

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(pi => pi.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(pi => pi.Organization)
            .WithMany()
            .HasForeignKey(pi => pi.OrganizationId)
            .HasConstraintName("fk_purchase_invoices_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.SupplierId)
            .HasColumnName("supplier_id")
            .IsRequired();

        builder.HasOne(pi => pi.Supplier)
            .WithMany()
            .HasForeignKey(pi => pi.SupplierId)
            .HasConstraintName("fk_purchase_invoices_supplier")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(pi => pi.Farm)
            .WithMany()
            .HasForeignKey(pi => pi.FarmId)
            .HasConstraintName("fk_purchase_invoices_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.SupplierInvoiceNumber)
            .HasColumnName("supplier_invoice_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pi => pi.InvoiceDate)
            .HasColumnName("invoice_date")
            .IsRequired();

        builder.Property(pi => pi.DueDate)
            .HasColumnName("due_date");

        builder.Property(pi => pi.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(pi => pi.Currency)
            .WithMany()
            .HasForeignKey(pi => pi.CurrencyId)
            .HasConstraintName("fk_purchase_invoices_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.PaymentTerms)
            .HasColumnName("payment_terms")
            .HasMaxLength(200);

        builder.Property(pi => pi.Subtotal)
            .HasColumnName("subtotal")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pi => pi.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pi => pi.OtherCharges)
            .HasColumnName("other_charges")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pi => pi.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pi => pi.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pi => pi.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<PurchaseInvoiceStatus>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(pi => pi.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(pi => pi.AttachmentReference)
            .HasColumnName("attachment_reference")
            .HasMaxLength(500);

        builder.Property(pi => pi.PostedAt)
            .HasColumnName("posted_at");

        builder.Property(pi => pi.PostedBy)
            .HasColumnName("posted_by");

        builder.Property(pi => pi.ReversedAt)
            .HasColumnName("reversed_at");

        builder.Property(pi => pi.ReversedBy)
            .HasColumnName("reversed_by");

        builder.Property(pi => pi.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasMaxLength(500);

        builder.Property(pi => pi.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pi => pi.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(pi => pi.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(pi => pi.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasMany(pi => pi.Lines)
            .WithOne(line => line.PurchaseInvoice)
            .HasForeignKey(line => line.PurchaseInvoiceId)
            .HasConstraintName("fk_purchase_invoice_lines_invoice")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pi => pi.PaymentAllocations)
            .WithOne(alloc => alloc.PurchaseInvoice)
            .HasForeignKey(alloc => alloc.PurchaseInvoiceId)
            .HasConstraintName("fk_supplier_payment_allocations_invoice")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pi => pi.ReceiptLines)
            .WithOne(rcpt => rcpt.PurchaseInvoice)
            .HasForeignKey(rcpt => rcpt.PurchaseInvoiceId)
            .HasConstraintName("fk_purchase_invoice_receipt_lines_invoice")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pi => pi.OrganizationId)
            .HasDatabaseName("ix_purchase_invoices_organization_id");

        builder.HasIndex(pi => new { pi.OrganizationId, pi.SupplierId, pi.SupplierInvoiceNumber })
            .HasDatabaseName("ix_purchase_invoices_org_supplier_invoice_number")
            .HasFilter("status != 'REVERSED'")
            .IsUnique();

        builder.HasIndex(pi => new { pi.OrganizationId, pi.FarmId, pi.InvoiceDate })
            .HasDatabaseName("ix_purchase_invoices_org_farm_date");

        builder.HasIndex(pi => new { pi.OrganizationId, pi.Status })
            .HasDatabaseName("ix_purchase_invoices_org_status");
    }
}
