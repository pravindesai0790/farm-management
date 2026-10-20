using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FertilizerOperationIdempotencyRecordConfiguration : IEntityTypeConfiguration<FertilizerOperationIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<FertilizerOperationIdempotencyRecord> builder)
    {
        builder.ToTable("fertilizer_operation_idempotency_records");

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

        builder.Property(r => r.FertilizerApplicationId)
            .HasColumnName("fertilizer_application_id")
            .IsRequired();

        builder.Property(r => r.RequestHash)
            .HasColumnName("request_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(r => r.Organization)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.FertilizerApplication)
            .WithMany()
            .HasForeignKey(r => r.FertilizerApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.OrganizationId, r.OperationType, r.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_fertilizer_operation_idempotency");

        builder.HasIndex(r => r.FertilizerApplicationId)
            .HasDatabaseName("ix_fertilizer_operation_idempotency_app_id");
    }
}
