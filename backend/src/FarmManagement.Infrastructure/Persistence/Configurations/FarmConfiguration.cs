using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("farms");

        builder.HasKey(farm => farm.Id);

        builder.Property(farm => farm.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(farm => farm.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(farm => farm.Organization)
            .WithMany()
            .HasForeignKey(farm => farm.OrganizationId)
            .HasConstraintName("fk_farm_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(farm => farm.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(farm => farm.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(farm => farm.Description)
            .HasColumnName("description");

        builder.Property(farm => farm.OwnershipTypeId)
            .HasColumnName("ownership_type_id")
            .IsRequired();

        builder.HasOne(farm => farm.OwnershipType)
            .WithMany()
            .HasForeignKey(farm => farm.OwnershipTypeId)
            .HasConstraintName("fk_farm_ownership_type")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(farm => farm.TotalArea)
            .HasColumnName("total_area")
            .HasPrecision(18, 4);

        builder.Property(farm => farm.AreaUnitId)
            .HasColumnName("area_unit_id");

        builder.HasOne(farm => farm.AreaUnit)
            .WithMany()
            .HasForeignKey(farm => farm.AreaUnitId)
            .HasConstraintName("fk_farm_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(farm => farm.AddressLine1).HasColumnName("address_line1").HasMaxLength(250);
        builder.Property(farm => farm.AddressLine2).HasColumnName("address_line2").HasMaxLength(250);
        builder.Property(farm => farm.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(farm => farm.District).HasColumnName("district").HasMaxLength(100);
        builder.Property(farm => farm.State).HasColumnName("state").HasMaxLength(100);
        builder.Property(farm => farm.Country).HasColumnName("country").HasMaxLength(100);
        builder.Property(farm => farm.PostalCode).HasColumnName("postal_code").HasMaxLength(30);
        builder.Property(farm => farm.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
        builder.Property(farm => farm.Longitude).HasColumnName("longitude").HasPrecision(10, 7);

        builder.Property(farm => farm.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(farm => farm.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(farm => farm.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(farm => farm.UpdatedAt).HasColumnName("updated_at");
        builder.Property(farm => farm.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(farm => new { farm.OrganizationId, farm.Code })
            .HasDatabaseName("ux_farm_organization_code")
            .IsUnique();
    }
}
