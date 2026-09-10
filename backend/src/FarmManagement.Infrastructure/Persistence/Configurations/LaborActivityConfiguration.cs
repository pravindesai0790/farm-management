using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class LaborActivityConfiguration : IEntityTypeConfiguration<LaborActivity>
{
    public void Configure(EntityTypeBuilder<LaborActivity> builder)
    {
        builder.ToTable("labor_activities", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_labor_activities_worker_count",
                "worker_count > 0");

            tableBuilder.HasCheckConstraint(
                "ck_labor_activities_working_hours",
                "total_working_hours IS NULL OR total_working_hours > 0");

            tableBuilder.HasCheckConstraint(
                "ck_labor_activities_cost_amount",
                "cost_amount IS NULL OR cost_amount >= 0");

            tableBuilder.HasCheckConstraint(
                "ck_labor_activities_status",
                "status IN ('DRAFT', 'COMPLETED', 'CANCELLED')");
        });

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(activity => activity.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(activity => activity.Organization)
            .WithMany()
            .HasForeignKey(activity => activity.OrganizationId)
            .HasConstraintName("fk_labor_activity_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.ActivityDate)
            .HasColumnName("activity_date")
            .IsRequired();

        builder.Property(activity => activity.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(activity => activity.Farm)
            .WithMany()
            .HasForeignKey(activity => activity.FarmId)
            .HasConstraintName("fk_labor_activity_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.FarmAreaId)
            .HasColumnName("farm_area_id");

        builder.HasOne(activity => activity.FarmArea)
            .WithMany()
            .HasForeignKey(activity => activity.FarmAreaId)
            .HasConstraintName("fk_labor_activity_area")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.PlantationId)
            .HasColumnName("plantation_id");

        builder.HasOne(activity => activity.Plantation)
            .WithMany()
            .HasForeignKey(activity => activity.PlantationId)
            .HasConstraintName("fk_labor_activity_plantation")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.CropCycleId)
            .HasColumnName("crop_cycle_id");

        builder.HasOne(activity => activity.CropCycle)
            .WithMany()
            .HasForeignKey(activity => activity.CropCycleId)
            .HasConstraintName("fk_labor_activity_cycle")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.LaborActivityTypeId)
            .HasColumnName("labor_activity_type_id")
            .IsRequired();

        builder.HasOne(activity => activity.LaborActivityType)
            .WithMany()
            .HasForeignKey(activity => activity.LaborActivityTypeId)
            .HasConstraintName("fk_labor_activity_type")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(activity => activity.Description)
            .HasColumnName("description");

        builder.Property(activity => activity.WorkerCount)
            .HasColumnName("worker_count")
            .IsRequired();

        builder.Property(activity => activity.TotalWorkingHours)
            .HasColumnName("total_working_hours")
            .HasPrecision(10, 2);

        builder.Property(activity => activity.CostAmount)
            .HasColumnName("cost_amount")
            .HasPrecision(18, 2);

        builder.Property(activity => activity.CurrencyId)
            .HasColumnName("currency_id");

        builder.Property(activity => activity.Status)
            .HasColumnName("status")
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<LaborActivityStatus>(value, true))
            .HasMaxLength(30)
            .HasDefaultValue(LaborActivityStatus.Completed)
            .IsRequired();

        builder.Property(activity => activity.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(activity => activity.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(activity => activity.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(activity => activity.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(activity => activity.OrganizationId)
            .HasDatabaseName("ix_labor_activities_organization");

        builder.HasIndex(activity => activity.FarmId)
            .HasDatabaseName("ix_labor_activities_farm");

        builder.HasIndex(activity => activity.ActivityDate)
            .HasDatabaseName("ix_labor_activities_activity_date");

        builder.HasIndex(activity => activity.PlantationId)
            .HasDatabaseName("ix_labor_activities_plantation");

        builder.HasIndex(activity => activity.CropCycleId)
            .HasDatabaseName("ix_labor_activities_crop_cycle");

        builder.HasIndex(activity => activity.FarmAreaId)
            .HasDatabaseName("ix_labor_activities_farm_area");

        builder.HasIndex(activity => activity.LaborActivityTypeId)
            .HasDatabaseName("ix_labor_activities_type");

        builder.HasIndex(activity => activity.Status)
            .HasDatabaseName("ix_labor_activities_status");
    }
}
