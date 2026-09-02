using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(unit => unit.OrganizationId)
            .HasColumnName("organization_id");

        builder.HasOne(unit => unit.Organization)
            .WithMany()
            .HasForeignKey(unit => unit.OrganizationId)
            .HasConstraintName("fk_units_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(unit => unit.Code)
            .HasColumnName("code")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(unit => unit.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(unit => unit.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(unit => unit.UnitCategory)
            .HasColumnName("unit_category")
            .HasConversion(
                unitCategory => unitCategory.ToString().ToUpperInvariant(),
                value => Enum.Parse<UnitCategory>(value, ignoreCase: true))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(unit => unit.BaseUnitCode)
            .HasColumnName("base_unit_code")
            .HasMaxLength(30);

        builder.Property(unit => unit.ConversionFactor)
            .HasColumnName("conversion_factor")
            .HasPrecision(18, 8);

        builder.Property(unit => unit.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(unit => unit.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(unit => unit.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(unit => unit.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(unit => unit.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(unit => unit.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(unit => unit.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(unit => new { unit.OrganizationId, unit.Code })
            .HasDatabaseName("ux_units_organization_code")
            .IsUnique();

        builder.HasIndex(unit => unit.Code)
            .HasDatabaseName("ux_units_system_code")
            .HasFilter("organization_id IS NULL")
            .IsUnique();

        builder.HasIndex(unit => new { unit.OrganizationId, unit.UnitCategory })
            .HasDatabaseName("ix_units_organization_category");
    }
}
