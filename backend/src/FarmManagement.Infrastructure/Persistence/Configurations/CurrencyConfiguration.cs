using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(currency => currency.Id);

        builder.Property(currency => currency.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(currency => currency.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(currency => currency.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(currency => currency.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(currency => currency.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(currency => currency.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(currency => currency.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(currency => currency.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(currency => currency.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(currency => currency.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(currency => currency.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(currency => currency.Code)
            .HasDatabaseName("ux_currencies_code")
            .IsUnique();
    }
}
