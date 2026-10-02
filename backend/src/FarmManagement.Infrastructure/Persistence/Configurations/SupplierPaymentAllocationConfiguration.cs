using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SupplierPaymentAllocationConfiguration : IEntityTypeConfiguration<SupplierPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentAllocation> builder)
    {
        builder.ToTable("supplier_payment_allocations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_supplier_payment_allocations_amount", "allocated_amount > 0");
        });

        builder.HasKey(spa => spa.Id);

        builder.Property(spa => spa.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(spa => spa.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(spa => spa.Organization)
            .WithMany()
            .HasForeignKey(spa => spa.OrganizationId)
            .HasConstraintName("fk_supplier_payment_allocations_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(spa => spa.SupplierPaymentId)
            .HasColumnName("supplier_payment_id")
            .IsRequired();

        builder.Property(spa => spa.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id")
            .IsRequired();

        builder.Property(spa => spa.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(spa => spa.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(spa => spa.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasIndex(spa => spa.SupplierPaymentId)
            .HasDatabaseName("ix_supplier_payment_allocations_payment_id");

        builder.HasIndex(spa => spa.PurchaseInvoiceId)
            .HasDatabaseName("ix_supplier_payment_allocations_invoice_id");

        builder.HasIndex(spa => spa.OrganizationId)
            .HasDatabaseName("ix_supplier_payment_allocations_org_id");
    }
}
