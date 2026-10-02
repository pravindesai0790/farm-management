using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .HasConstraintName("fk_suppliers_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(150);

        builder.Property(s => s.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .HasColumnName("email")
            .HasMaxLength(150);

        builder.Property(s => s.Address)
            .HasColumnName("address")
            .HasMaxLength(500);

        builder.Property(s => s.RegistrationIdentifier)
            .HasColumnName("registration_identifier")
            .HasMaxLength(100);

        builder.Property(s => s.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(s => s.OrganizationId)
            .HasDatabaseName("ix_suppliers_organization_id");

        builder.HasIndex(s => new { s.OrganizationId, s.Name })
            .HasDatabaseName("ix_suppliers_organization_name");
    }
}
