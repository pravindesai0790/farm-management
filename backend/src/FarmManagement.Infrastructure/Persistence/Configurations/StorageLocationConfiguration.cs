using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.ToTable("storage_locations");

        builder.HasKey(location => location.Id);

        builder.Property(location => location.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(location => location.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(location => location.Organization)
            .WithMany()
            .HasForeignKey(location => location.OrganizationId)
            .HasConstraintName("fk_storage_locations_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(location => location.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(location => location.Farm)
            .WithMany()
            .HasForeignKey(location => location.FarmId)
            .HasConstraintName("fk_storage_locations_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(location => location.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(location => location.Description)
            .HasColumnName("description");

        builder.Property(location => location.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(location => location.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(location => location.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(location => location.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(location => location.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(location => new { location.FarmId, location.Name })
            .HasDatabaseName("ux_storage_locations_farm_name")
            .IsUnique();

        builder.HasIndex(location => new { location.OrganizationId, location.FarmId })
            .HasDatabaseName("ix_storage_locations_organization_farm");
    }
}
