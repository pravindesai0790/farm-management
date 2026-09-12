using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborAttendanceConfiguration : IEntityTypeConfiguration<LaborAttendance>
{
    public void Configure(EntityTypeBuilder<LaborAttendance> builder)
    {
        builder.ToTable("labor_attendance", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_type",
                "attendance_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'NOT_WORKED')");

            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_status",
                "status IN ('DRAFT', 'FINALIZED')");

            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_working_hours",
                "(attendance_type = 'HOURLY' AND working_hours IS NOT NULL AND working_hours > 0) OR (attendance_type != 'HOURLY' AND working_hours IS NULL)");

            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_not_worked_earnings",
                "attendance_type != 'NOT_WORKED' OR calculated_amount = 0 OR calculated_amount IS NULL");

            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_amounts",
                "(calculated_rate IS NULL OR calculated_rate >= 0) AND (calculated_amount IS NULL OR calculated_amount >= 0)");

            tableBuilder.HasCheckConstraint(
                "ck_labor_attendance_finalized",
                "status != 'FINALIZED' OR (finalized_at IS NOT NULL AND finalized_by IS NOT NULL)");
        });

        builder.HasKey(attendance => attendance.Id);

        builder.Property(attendance => attendance.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(attendance => attendance.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(attendance => attendance.Organization)
            .WithMany()
            .HasForeignKey(attendance => attendance.OrganizationId)
            .HasConstraintName("fk_labor_attendance_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(attendance => attendance.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(attendance => attendance.Farm)
            .WithMany()
            .HasForeignKey(attendance => attendance.FarmId)
            .HasConstraintName("fk_labor_attendance_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(attendance => attendance.WorkerId)
            .HasColumnName("worker_id")
            .IsRequired();

        builder.HasOne(attendance => attendance.Worker)
            .WithMany()
            .HasForeignKey(attendance => attendance.WorkerId)
            .HasConstraintName("fk_labor_attendance_worker")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(attendance => attendance.AttendanceDate)
            .HasColumnName("attendance_date")
            .IsRequired();

        builder.Property(attendance => attendance.AttendanceType)
            .HasColumnName("attendance_type")
            .HasConversion(
                attendanceType => ToStringValue(attendanceType),
                value => FromStringValue(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(attendance => attendance.WorkingHours)
            .HasColumnName("working_hours")
            .HasPrecision(18, 2);

        builder.Property(attendance => attendance.CalculatedRate)
            .HasColumnName("calculated_rate")
            .HasPrecision(18, 2);

        builder.Property(attendance => attendance.CalculatedAmount)
            .HasColumnName("calculated_amount")
            .HasPrecision(18, 2);

        builder.Property(attendance => attendance.CurrencyId)
            .HasColumnName("currency_id");

        builder.HasOne(attendance => attendance.Currency)
            .WithMany()
            .HasForeignKey(attendance => attendance.CurrencyId)
            .HasConstraintName("fk_labor_attendance_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(attendance => attendance.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ToStringValue(status),
                value => StatusFromStringValue(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(attendance => attendance.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(attendance => attendance.FinalizedAt)
            .HasColumnName("finalized_at");

        builder.Property(attendance => attendance.FinalizedBy)
            .HasColumnName("finalized_by");

        builder.Property(attendance => attendance.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(attendance => attendance.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(attendance => attendance.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(attendance => attendance.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(attendance => attendance.OrganizationId)
            .HasDatabaseName("ix_labor_attendance_organization_id");

        builder.HasIndex(attendance => new { attendance.OrganizationId, attendance.AttendanceDate })
            .HasDatabaseName("ix_labor_attendance_org_attendance_date");

        builder.HasIndex(attendance => new { attendance.OrganizationId, attendance.FarmId, attendance.AttendanceDate })
            .HasDatabaseName("ix_labor_attendance_org_farm_attendance_date");

        builder.HasIndex(new[] { "OrganizationId", "WorkerId", "AttendanceDate" }, "ix_labor_attendance_org_worker_attendance_date");

        builder.HasIndex(attendance => attendance.Status)
            .HasDatabaseName("ix_labor_attendance_status");

        builder.HasIndex(attendance => new { attendance.FarmId, attendance.AttendanceDate })
            .HasDatabaseName("ix_labor_attendance_farm_attendance_date");

        builder.HasIndex(new[] { "OrganizationId", "WorkerId", "AttendanceDate" }, "ux_labor_attendance_org_worker_date_payroll")
            .HasFilter("attendance_type != 'NOT_WORKED'")
            .IsUnique();
    }

    private static string ToStringValue(AttendanceType attendanceType) => attendanceType switch
    {
        AttendanceType.FullDay => "FULL_DAY",
        AttendanceType.HalfDay => "HALF_DAY",
        AttendanceType.Hourly => "HOURLY",
        AttendanceType.NotWorked => "NOT_WORKED",
        _ => throw new ArgumentOutOfRangeException(nameof(attendanceType), attendanceType, null)
    };

    private static AttendanceType FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "FULL_DAY" or "FULLDAY" => AttendanceType.FullDay,
        "HALF_DAY" or "HALFDAY" => AttendanceType.HalfDay,
        "HOURLY" => AttendanceType.Hourly,
        "NOT_WORKED" or "NOTWORKED" => AttendanceType.NotWorked,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    private static string ToStringValue(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Draft => "DRAFT",
        AttendanceStatus.Finalized => "FINALIZED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static AttendanceStatus StatusFromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "DRAFT" => AttendanceStatus.Draft,
        "FINALIZED" => AttendanceStatus.Finalized,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
