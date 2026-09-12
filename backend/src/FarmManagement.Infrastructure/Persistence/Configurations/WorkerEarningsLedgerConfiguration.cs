using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkerEarningsLedgerConfiguration : IEntityTypeConfiguration<WorkerEarningsLedger>
{
    public void Configure(EntityTypeBuilder<WorkerEarningsLedger> builder)
    {
        builder.ToTable("worker_earnings_ledger", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_entry_type",
                "entry_type IN ('EARNING', 'REVERSAL', 'ADJUSTMENT')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_status",
                "status IN ('CALCULATED', 'APPROVED', 'REVERSED')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_wage_type",
                "wage_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'MONTHLY')");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_quantity",
                "quantity > 0");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_wage_rate",
                "wage_rate > 0");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_earning_amount",
                "entry_type != 'EARNING' OR gross_amount > 0");

            tableBuilder.HasCheckConstraint(
                "ck_worker_earnings_ledger_reversal_ref",
                "entry_type != 'REVERSAL' OR reference_ledger_id IS NOT NULL");
        });

        builder.HasKey(ledger => ledger.Id);

        builder.Property(ledger => ledger.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(ledger => ledger.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(ledger => ledger.Organization)
            .WithMany()
            .HasForeignKey(ledger => ledger.OrganizationId)
            .HasConstraintName("fk_worker_earnings_ledger_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ledger => ledger.WorkerId)
            .HasColumnName("worker_id")
            .IsRequired();

        builder.HasOne(ledger => ledger.Worker)
            .WithMany(worker => worker.EarningsLedgerEntries)
            .HasForeignKey(ledger => ledger.WorkerId)
            .HasConstraintName("fk_worker_earnings_ledger_worker")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ledger => ledger.AttendanceId)
            .HasColumnName("attendance_id");

        builder.Property(ledger => ledger.EarningsDate)
            .HasColumnName("earnings_date")
            .IsRequired();

        builder.Property(ledger => ledger.WageType)
            .HasColumnName("wage_type")
            .HasConversion(
                wageType => ConvertWageTypeToString(wageType),
                value => ConvertStringToWageType(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ledger => ledger.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ledger => ledger.WageRate)
            .HasColumnName("wage_rate")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ledger => ledger.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(ledger => ledger.Currency)
            .WithMany()
            .HasForeignKey(ledger => ledger.CurrencyId)
            .HasConstraintName("fk_worker_earnings_ledger_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ledger => ledger.GrossAmount)
            .HasColumnName("gross_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ledger => ledger.EntryType)
            .HasColumnName("entry_type")
            .HasConversion(
                entryType => ConvertEntryTypeToString(entryType),
                value => ConvertStringToEntryType(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ledger => ledger.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ConvertStatusToString(status),
                value => ConvertStringToStatus(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ledger => ledger.ReferenceLedgerId)
            .HasColumnName("reference_ledger_id");

        builder.HasOne(ledger => ledger.ReferenceLedger)
            .WithMany()
            .HasForeignKey(ledger => ledger.ReferenceLedgerId)
            .HasConstraintName("fk_worker_earnings_ledger_reference")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ledger => ledger.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(ledger => ledger.FinalizedAt)
            .HasColumnName("finalized_at");

        builder.Property(ledger => ledger.FinalizedBy)
            .HasColumnName("finalized_by");

        builder.Property(ledger => ledger.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ledger => ledger.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(ledger => ledger.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ledger => ledger.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(ledger => ledger.OrganizationId)
            .HasDatabaseName("ix_worker_earnings_ledger_organization_id");

        builder.HasIndex(ledger => new { ledger.OrganizationId, ledger.WorkerId, ledger.EarningsDate })
            .HasDatabaseName("ix_worker_earnings_ledger_org_worker_date");

        builder.HasIndex(ledger => new { ledger.OrganizationId, ledger.WorkerId, ledger.Status })
            .HasDatabaseName("ix_worker_earnings_ledger_org_worker_status");

        builder.HasIndex(ledger => new { ledger.OrganizationId, ledger.AttendanceId })
            .HasDatabaseName("ix_worker_earnings_ledger_org_attendance");

        builder.HasIndex(ledger => new { ledger.OrganizationId, ledger.ReferenceLedgerId })
            .HasDatabaseName("ix_worker_earnings_ledger_org_reference");
    }

    private static string ConvertWageTypeToString(WageType wageType) => wageType switch
    {
        WageType.FullDay => "FULL_DAY",
        WageType.HalfDay => "HALF_DAY",
        WageType.Hourly => "HOURLY",
        WageType.Monthly => "MONTHLY",
        _ => wageType.ToString().ToUpperInvariant()
    };

    private static WageType ConvertStringToWageType(string value) => value.ToUpperInvariant() switch
    {
        "FULL_DAY" or "FULLDAY" => WageType.FullDay,
        "HALF_DAY" or "HALFDAY" => WageType.HalfDay,
        "HOURLY" => WageType.Hourly,
        "MONTHLY" => WageType.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ConvertEntryTypeToString(EarningsEntryType entryType) => entryType switch
    {
        EarningsEntryType.Earning => "EARNING",
        EarningsEntryType.Reversal => "REVERSAL",
        EarningsEntryType.Adjustment => "ADJUSTMENT",
        _ => entryType.ToString().ToUpperInvariant()
    };

    private static EarningsEntryType ConvertStringToEntryType(string value) => value.ToUpperInvariant() switch
    {
        "EARNING" => EarningsEntryType.Earning,
        "REVERSAL" => EarningsEntryType.Reversal,
        "ADJUSTMENT" => EarningsEntryType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ConvertStatusToString(EarningsLedgerStatus status) => status switch
    {
        EarningsLedgerStatus.Calculated => "CALCULATED",
        EarningsLedgerStatus.Approved => "APPROVED",
        EarningsLedgerStatus.Reversed => "REVERSED",
        _ => status.ToString().ToUpperInvariant()
    };

    private static EarningsLedgerStatus ConvertStringToStatus(string value) => value.ToUpperInvariant() switch
    {
        "CALCULATED" => EarningsLedgerStatus.Calculated,
        "APPROVED" => EarningsLedgerStatus.Approved,
        "REVERSED" => EarningsLedgerStatus.Reversed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
