using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(auditLog => auditLog.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(auditLog => auditLog.Organization)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.OrganizationId)
            .HasConstraintName("fk_audit_logs_organization")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(auditLog => auditLog.UserId)
            .HasColumnName("user_id");

        builder.HasOne(auditLog => auditLog.User)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(auditLog => auditLog.UserId)
            .HasConstraintName("fk_audit_logs_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(auditLog => auditLog.Action)
            .HasColumnName("action")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(150);

        builder.Property(auditLog => auditLog.EntityId)
            .HasColumnName("entity_id");

        builder.Property(auditLog => auditLog.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(auditLog => auditLog.OrganizationId)
            .HasDatabaseName("ix_audit_logs_organization_id");
    }
}
