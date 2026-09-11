using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_workers_gender",
                "gender IN ('MALE', 'FEMALE', 'OTHER')");

            tableBuilder.HasCheckConstraint(
                "ck_workers_employment_type",
                "employment_type IN ('PERMANENT', 'SEASONAL', 'DAILY_WAGE', 'CONTRACT')");

            tableBuilder.HasCheckConstraint(
                "ck_workers_contractor_required",
                "employment_type != 'CONTRACT' OR contractor_id IS NOT NULL");

            tableBuilder.HasCheckConstraint(
                "ck_workers_dates",
                "leaving_date IS NULL OR joining_date IS NULL OR leaving_date >= joining_date");
        });

        builder.HasKey(worker => worker.Id);

        builder.Property(worker => worker.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(worker => worker.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(worker => worker.Organization)
            .WithMany()
            .HasForeignKey(worker => worker.OrganizationId)
            .HasConstraintName("fk_workers_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(worker => worker.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(worker => worker.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);

        builder.Property(worker => worker.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(worker => worker.Gender)
            .HasColumnName("gender")
            .HasConversion(
                gender => gender.ToString().ToUpperInvariant(),
                value => Enum.Parse<Gender>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(worker => worker.MobileNumber)
            .HasColumnName("mobile_number")
            .HasMaxLength(30);

        builder.Property(worker => worker.AlternateMobileNumber)
            .HasColumnName("alternate_mobile_number")
            .HasMaxLength(30);

        builder.Property(worker => worker.LaborCategoryId)
            .HasColumnName("labor_category_id");

        builder.HasOne(worker => worker.LaborCategory)
            .WithMany(category => category.Workers)
            .HasForeignKey(worker => worker.LaborCategoryId)
            .HasConstraintName("fk_workers_labor_category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(worker => worker.EmploymentType)
            .HasColumnName("employment_type")
            .HasConversion(
                type => ConvertEmploymentTypeToString(type),
                value => ConvertStringToEmploymentType(value))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(worker => worker.ContractorId)
            .HasColumnName("contractor_id");

        builder.HasOne(worker => worker.Contractor)
            .WithMany(contractor => contractor.Workers)
            .HasForeignKey(worker => worker.ContractorId)
            .HasConstraintName("fk_workers_contractor")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(worker => worker.JoiningDate)
            .HasColumnName("joining_date");

        builder.Property(worker => worker.LeavingDate)
            .HasColumnName("leaving_date");

        builder.Property(worker => worker.Notes)
            .HasColumnName("notes");

        builder.Property(worker => worker.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(worker => worker.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(worker => worker.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(worker => worker.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(worker => worker.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(worker => worker.OrganizationId)
            .HasDatabaseName("ix_workers_organization_id");

        builder.HasIndex(worker => new { worker.OrganizationId, worker.DisplayName })
            .HasDatabaseName("ix_workers_organization_display_name");

        builder.HasIndex(worker => new { worker.OrganizationId, worker.MobileNumber })
            .HasDatabaseName("ix_workers_organization_mobile");

        builder.HasIndex(worker => new { worker.OrganizationId, worker.IsActive })
            .HasDatabaseName("ix_workers_organization_is_active");

        builder.HasIndex(worker => new { worker.OrganizationId, worker.ContractorId })
            .HasDatabaseName("ix_workers_organization_contractor");

        builder.HasIndex(worker => new { worker.OrganizationId, worker.LaborCategoryId })
            .HasDatabaseName("ix_workers_organization_labor_category");
    }

    private static string ConvertEmploymentTypeToString(EmploymentType type) => type switch
    {
        EmploymentType.Permanent => "PERMANENT",
        EmploymentType.Seasonal => "SEASONAL",
        EmploymentType.DailyWage => "DAILY_WAGE",
        EmploymentType.Contract => "CONTRACT",
        _ => type.ToString().ToUpperInvariant()
    };

    private static EmploymentType ConvertStringToEmploymentType(string value) => value.ToUpperInvariant() switch
    {
        "PERMANENT" => EmploymentType.Permanent,
        "SEASONAL" => EmploymentType.Seasonal,
        "DAILY_WAGE" or "DAILYWAGE" => EmploymentType.DailyWage,
        "CONTRACT" => EmploymentType.Contract,
        _ => Enum.Parse<EmploymentType>(value, true)
    };
}
