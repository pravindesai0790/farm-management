using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkerPaymentConfiguration : IEntityTypeConfiguration<WorkerPayment>
{
    public void Configure(EntityTypeBuilder<WorkerPayment> builder)
    {
        builder.ToTable("worker_payments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_worker_payments_payment_type",
                "payment_type IN ('ADVANCE', 'PAYOUT', 'ADJUSTMENT')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_payments_status",
                "status IN ('PENDING', 'COMPLETED', 'CANCELLED')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_payments_payment_method",
                "payment_method IN ('CASH', 'BANK_TRANSFER', 'UPI', 'CHEQUE', 'OTHER')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_payments_amount",
                "amount > 0");

            tableBuilder.HasCheckConstraint(
                "ck_worker_payments_period",
                "payment_period_to IS NULL OR payment_period_from IS NULL OR payment_period_to >= payment_period_from");
        });

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(payment => payment.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(payment => payment.Organization)
            .WithMany()
            .HasForeignKey(payment => payment.OrganizationId)
            .HasConstraintName("fk_worker_payments_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(payment => payment.WorkerId)
            .HasColumnName("worker_id")
            .IsRequired();

        builder.HasOne(payment => payment.Worker)
            .WithMany(worker => worker.Payments)
            .HasForeignKey(payment => payment.WorkerId)
            .HasConstraintName("fk_worker_payments_worker")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(payment => payment.PaymentDate)
            .HasColumnName("payment_date")
            .IsRequired();

        builder.Property(payment => payment.PaymentType)
            .HasColumnName("payment_type")
            .HasConversion(
                type => ConvertPaymentTypeToString(type),
                value => ConvertStringToPaymentType(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(payment => payment.Currency)
            .WithMany()
            .HasForeignKey(payment => payment.CurrencyId)
            .HasConstraintName("fk_worker_payments_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(payment => payment.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion(
                method => ConvertPaymentMethodToString(method),
                value => ConvertStringToPaymentMethod(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(100);

        builder.Property(payment => payment.PaymentPeriodFrom)
            .HasColumnName("payment_period_from");

        builder.Property(payment => payment.PaymentPeriodTo)
            .HasColumnName("payment_period_to");

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ConvertStatusToString(status),
                value => ConvertStringToStatus(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(payment => payment.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(payment => payment.CancelledBy)
            .HasColumnName("cancelled_by");

        builder.Property(payment => payment.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text");

        builder.Property(payment => payment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(payment => payment.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(payment => payment.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(payment => payment.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(payment => payment.OrganizationId)
            .HasDatabaseName("ix_worker_payments_organization_id");

        builder.HasIndex(payment => new { payment.OrganizationId, payment.WorkerId, payment.PaymentDate })
            .HasDatabaseName("ix_worker_payments_org_worker_date");

        builder.HasIndex(payment => new { payment.OrganizationId, payment.WorkerId, payment.Status })
            .HasDatabaseName("ix_worker_payments_org_worker_status");
    }

    private static string ConvertPaymentTypeToString(PaymentType type) => type switch
    {
        PaymentType.Advance => "ADVANCE",
        PaymentType.Payout => "PAYOUT",
        PaymentType.Adjustment => "ADJUSTMENT",
        _ => type.ToString().ToUpperInvariant()
    };

    private static PaymentType ConvertStringToPaymentType(string value) => value.ToUpperInvariant() switch
    {
        "ADVANCE" => PaymentType.Advance,
        "PAYOUT" => PaymentType.Payout,
        "ADJUSTMENT" => PaymentType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

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
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ConvertStatusToString(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "PENDING",
        PaymentStatus.Completed => "COMPLETED",
        PaymentStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant()
    };

    private static PaymentStatus ConvertStringToStatus(string value) => value.ToUpperInvariant() switch
    {
        "PENDING" => PaymentStatus.Pending,
        "COMPLETED" => PaymentStatus.Completed,
        "CANCELLED" => PaymentStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
