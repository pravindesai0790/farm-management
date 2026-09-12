using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkerPaymentAllocationConfiguration : IEntityTypeConfiguration<WorkerPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<WorkerPaymentAllocation> builder)
    {
        builder.ToTable("worker_payment_allocations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(x => x.WorkerPaymentId)
            .HasColumnName("worker_payment_id")
            .IsRequired();

        builder.Property(x => x.WorkerEarningsLedgerId)
            .HasColumnName("worker_earnings_ledger_id")
            .IsRequired(false);

        builder.Property(x => x.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.AllocationType)
            .HasColumnName("allocation_type")
            .HasMaxLength(32)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<PaymentAllocationType>(v, true))
            .IsRequired();

        builder.Property(x => x.AllocationDate)
            .HasColumnName("allocation_date")
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by")
            .IsRequired(false);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkerPayment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(x => x.WorkerPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.WorkerEarningsLedger)
            .WithMany(e => e.PaymentAllocations)
            .HasForeignKey(x => x.WorkerEarningsLedgerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.WorkerPaymentId });
        builder.HasIndex(x => new { x.OrganizationId, x.WorkerEarningsLedgerId });
        builder.HasIndex(x => new { x.OrganizationId, x.AllocationDate });
        builder.HasIndex(x => new { x.OrganizationId, x.AllocationType });
    }
}
