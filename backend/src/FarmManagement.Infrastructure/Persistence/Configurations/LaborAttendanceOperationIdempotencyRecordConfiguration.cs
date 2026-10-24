using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborAttendanceOperationIdempotencyRecordConfiguration : IEntityTypeConfiguration<LaborAttendanceOperationIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<LaborAttendanceOperationIdempotencyRecord> builder)
    {
        builder.ToTable("labor_attendance_idempotency_records");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(r => r.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.RequestHash)
            .HasColumnName("request_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.ResponsePayload)
            .HasColumnName("response_payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.OrganizationId, r.OperationType, r.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_labor_attendance_operation_idempotency");

        builder.HasIndex(r => new { r.OrganizationId, r.CreatedAt })
            .HasDatabaseName("ix_labor_attendance_operation_idempotency_created");
    }
}
