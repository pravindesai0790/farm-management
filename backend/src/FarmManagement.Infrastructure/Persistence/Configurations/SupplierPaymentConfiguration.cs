using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.ToTable("supplier_payments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_supplier_payments_amount", "amount > 0");
            tableBuilder.HasCheckConstraint("ck_supplier_payments_status", "status IN ('COMPLETED', 'REVERSED')");
            tableBuilder.HasCheckConstraint(
                "ck_supplier_payments_payment_method",
                "payment_method IN ('CASH', 'BANK_TRANSFER', 'UPI', 'CHEQUE', 'OTHER')");
        });

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(sp => sp.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(sp => sp.Organization)
            .WithMany()
            .HasForeignKey(sp => sp.OrganizationId)
            .HasConstraintName("fk_supplier_payments_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sp => sp.SupplierId)
            .HasColumnName("supplier_id")
            .IsRequired();

        builder.HasOne(sp => sp.Supplier)
            .WithMany()
            .HasForeignKey(sp => sp.SupplierId)
            .HasConstraintName("fk_supplier_payments_supplier")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sp => sp.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(sp => sp.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(sp => sp.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(sp => sp.Currency)
            .WithMany()
            .HasForeignKey(sp => sp.CurrencyId)
            .HasConstraintName("fk_supplier_payments_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sp => sp.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion(
                method => ConvertPaymentMethodToString(method),
                value => ConvertStringToPaymentMethod(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(sp => sp.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(100);

        builder.Property(sp => sp.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(sp => sp.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<SupplierPaymentStatus>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sp => sp.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        builder.Property(sp => sp.ReversedAt)
            .HasColumnName("reversed_at");

        builder.Property(sp => sp.ReversedBy)
            .HasColumnName("reversed_by");

        builder.Property(sp => sp.ReversalReason)
            .HasColumnName("reversal_reason")
            .HasMaxLength(500);

        builder.Property(sp => sp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(sp => sp.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(sp => sp.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasMany(sp => sp.Allocations)
            .WithOne(alloc => alloc.SupplierPayment)
            .HasForeignKey(alloc => alloc.SupplierPaymentId)
            .HasConstraintName("fk_supplier_payment_allocations_payment")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => sp.OrganizationId)
            .HasDatabaseName("ix_supplier_payments_organization_id");

        builder.HasIndex(sp => new { sp.OrganizationId, sp.SupplierId, sp.PaymentDate })
            .HasDatabaseName("ix_supplier_payments_org_supplier_date");

        builder.HasIndex(sp => new { sp.OrganizationId, sp.IdempotencyKey })
            .HasDatabaseName("ix_supplier_payments_org_idempotency")
            .HasFilter("idempotency_key IS NOT NULL")
            .IsUnique();
    }

    private static string ConvertPaymentMethodToString(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "CASH",
        PaymentMethod.BankTransfer => "BANK_TRANSFER",
        PaymentMethod.Upi => "UPI",
        PaymentMethod.Cheque => "CHEQUE",
        PaymentMethod.Other => "OTHER",
        _ => method.ToString().ToUpperInvariant()
    };

    private static PaymentMethod ConvertStringToPaymentMethod(string value) => value.ToUpperInvariant() switch
    {
        "CASH" => PaymentMethod.Cash,
        "BANK_TRANSFER" or "BANKTRANSFER" => PaymentMethod.BankTransfer,
        "UPI" => PaymentMethod.Upi,
        "CHEQUE" => PaymentMethod.Cheque,
        "OTHER" => PaymentMethod.Other,
        _ => Enum.Parse<PaymentMethod>(value.Replace("_", ""), true)
    };
}
