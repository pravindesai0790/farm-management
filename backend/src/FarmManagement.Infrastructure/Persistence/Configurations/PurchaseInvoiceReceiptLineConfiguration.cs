using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for PurchaseInvoiceReceiptLine.
/// Configures table constraints, FK relationships, precision, and idempotency indexes.
/// </summary>
public sealed class PurchaseInvoiceReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceReceiptLine>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceReceiptLine> builder)
    {
        builder.ToTable("purchase_invoice_receipt_lines", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_purchase_invoice_receipt_lines_qty", "received_quantity > 0");
        });

        builder.HasKey(rl => rl.Id);

        builder.Property(rl => rl.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(rl => rl.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(rl => rl.Organization)
            .WithMany()
            .HasForeignKey(rl => rl.OrganizationId)
            .HasConstraintName("fk_purchase_invoice_receipt_lines_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rl => rl.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id")
            .IsRequired();

        builder.Property(rl => rl.PurchaseInvoiceLineId)
            .HasColumnName("purchase_invoice_line_id")
            .IsRequired();

        builder.HasOne(rl => rl.PurchaseInvoiceLine)
            .WithMany(line => line.ReceiptLines)
            .HasForeignKey(rl => rl.PurchaseInvoiceLineId)
            .HasConstraintName("fk_purchase_invoice_receipt_lines_line")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rl => rl.StockMovementId)
            .HasColumnName("stock_movement_id")
            .IsRequired();

        builder.HasOne(rl => rl.StockMovement)
            .WithMany()
            .HasForeignKey(rl => rl.StockMovementId)
            .HasConstraintName("fk_purchase_invoice_receipt_lines_movement")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rl => rl.ReceiptGroupId)
            .HasColumnName("receipt_group_id")
            .IsRequired();

        builder.Property(rl => rl.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(rl => rl.ReceivedQuantity)
            .HasColumnName("received_quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(rl => rl.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rl => rl.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasIndex(rl => rl.StockMovementId)
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_movement")
            .IsUnique();

        builder.HasIndex(rl => rl.PurchaseInvoiceLineId)
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_line_id");

        builder.HasIndex(rl => rl.PurchaseInvoiceId)
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_invoice_id");

        builder.HasIndex(rl => rl.OrganizationId)
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_org_id");

        builder.HasIndex(rl => rl.ReceiptGroupId)
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_group_id");

        builder.HasIndex(rl => new { rl.OrganizationId, rl.PurchaseInvoiceId, rl.IdempotencyKey })
            .HasDatabaseName("ix_purchase_invoice_receipt_lines_idempotency");
    }
}
