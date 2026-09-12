using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborWageRateConfiguration : IEntityTypeConfiguration<LaborWageRate>
{
    public void Configure(EntityTypeBuilder<LaborWageRate> builder)
    {
        builder.ToTable("labor_wage_rates", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_labor_wage_rates_wage_rate",
                "wage_rate > 0");

            tableBuilder.HasCheckConstraint(
                "ck_labor_wage_rates_dates",
                "effective_to IS NULL OR effective_to >= effective_from");

            tableBuilder.HasCheckConstraint(
                "ck_labor_wage_rates_gender",
                "gender IN ('MALE', 'FEMALE', 'OTHER')");

            tableBuilder.HasCheckConstraint(
                "ck_labor_wage_rates_wage_type",
                "wage_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'MONTHLY')");
        });

        builder.HasKey(rate => rate.Id);

        builder.Property(rate => rate.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(rate => rate.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(rate => rate.Organization)
            .WithMany()
            .HasForeignKey(rate => rate.OrganizationId)
            .HasConstraintName("fk_labor_wage_rates_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rate => rate.Gender)
            .HasColumnName("gender")
            .HasConversion(
                gender => gender.ToString().ToUpperInvariant(),
                value => Enum.Parse<Gender>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(rate => rate.WageType)
            .HasColumnName("wage_type")
            .HasConversion(
                wageType => ToStringValue(wageType),
                value => FromStringValue(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(rate => rate.WageRate)
            .HasColumnName("wage_rate")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(rate => rate.CurrencyId)
            .HasColumnName("currency_id")
            .IsRequired();

        builder.HasOne(rate => rate.Currency)
            .WithMany()
            .HasForeignKey(rate => rate.CurrencyId)
            .HasConstraintName("fk_labor_wage_rates_currency")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rate => rate.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(rate => rate.EffectiveTo)
            .HasColumnName("effective_to");

        builder.Property(rate => rate.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(rate => rate.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(rate => rate.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rate => rate.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(rate => rate.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(rate => rate.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(rate => rate.OrganizationId)
            .HasDatabaseName("ix_labor_wage_rates_organization_id");

        builder.HasIndex(rate => rate.CurrencyId)
            .HasDatabaseName("ix_labor_wage_rates_currency_id");

        builder.HasIndex(rate => new { rate.OrganizationId, rate.Gender, rate.WageType })
            .HasDatabaseName("ix_labor_wage_rates_org_gender_type");

        builder.HasIndex(rate => new { rate.OrganizationId, rate.Gender, rate.WageType, rate.IsActive })
            .HasDatabaseName("ix_labor_wage_rates_org_gender_type_active");

        builder.HasIndex(rate => new { rate.OrganizationId, rate.EffectiveFrom, rate.EffectiveTo })
            .HasDatabaseName("ix_labor_wage_rates_org_effective_dates");
    }

    private static string ToStringValue(WageType wageType) => wageType switch
    {
        WageType.FullDay => "FULL_DAY",
        WageType.HalfDay => "HALF_DAY",
        WageType.Hourly => "HOURLY",
        WageType.Monthly => "MONTHLY",
        _ => throw new ArgumentOutOfRangeException(nameof(wageType), wageType, null)
    };

    private static WageType FromStringValue(string value) => value.ToUpperInvariant() switch
    {
        "FULL_DAY" => WageType.FullDay,
        "HALF_DAY" => WageType.HalfDay,
        "HOURLY" => WageType.Hourly,
        "MONTHLY" => WageType.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
