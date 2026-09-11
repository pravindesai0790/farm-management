using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("contractors");

        builder.HasKey(contractor => contractor.Id);

        builder.Property(contractor => contractor.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(contractor => contractor.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(contractor => contractor.Organization)
            .WithMany()
            .HasForeignKey(contractor => contractor.OrganizationId)
            .HasConstraintName("fk_contractors_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contractor => contractor.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(contractor => contractor.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(150);

        builder.Property(contractor => contractor.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(50);

        builder.Property(contractor => contractor.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(contractor => contractor.Address)
            .HasColumnName("address");

        builder.Property(contractor => contractor.Notes)
            .HasColumnName("notes");

        builder.Property(contractor => contractor.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(contractor => contractor.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(contractor => contractor.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(contractor => contractor.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(contractor => contractor.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasMany(contractor => contractor.Workers)
            .WithOne(worker => worker.Contractor)
            .HasForeignKey(worker => worker.ContractorId)
            .HasConstraintName("fk_workers_contractor")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(contractor => contractor.OrganizationId)
            .HasDatabaseName("ix_contractors_organization_id");

        builder.HasIndex(contractor => new { contractor.OrganizationId, contractor.Name })
            .HasDatabaseName("ix_contractors_organization_name");

        builder.HasIndex(contractor => new { contractor.OrganizationId, contractor.IsActive })
            .HasDatabaseName("ix_contractors_organization_is_active");

        builder.HasIndex(contractor => new { contractor.OrganizationId, contractor.PhoneNumber })
            .HasDatabaseName("ix_contractors_organization_phone");
    }
}
