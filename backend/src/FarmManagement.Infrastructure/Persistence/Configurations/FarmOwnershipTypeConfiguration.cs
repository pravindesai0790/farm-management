using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FarmOwnershipTypeConfiguration : IEntityTypeConfiguration<FarmOwnershipType>
{
    public void Configure(EntityTypeBuilder<FarmOwnershipType> builder)
    {
        builder.ToTable("farm_ownership_types");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(item => item.Code)
            .HasDatabaseName("ux_farm_ownership_types_code")
            .IsUnique();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();
    }
}
