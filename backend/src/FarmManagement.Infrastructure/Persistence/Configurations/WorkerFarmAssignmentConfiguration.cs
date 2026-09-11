using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class WorkerFarmAssignmentConfiguration : IEntityTypeConfiguration<WorkerFarmAssignment>
{
    public void Configure(EntityTypeBuilder<WorkerFarmAssignment> builder)
    {
        builder.ToTable("worker_farm_assignments", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_worker_farm_assignments_dates",
                "assigned_to IS NULL OR assigned_to >= assigned_from");
        });

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(assignment => assignment.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(assignment => assignment.Organization)
            .WithMany()
            .HasForeignKey(assignment => assignment.OrganizationId)
            .HasConstraintName("fk_worker_farm_assignments_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.WorkerId)
            .HasColumnName("worker_id")
            .IsRequired();

        builder.HasOne(assignment => assignment.Worker)
            .WithMany(worker => worker.FarmAssignments)
            .HasForeignKey(assignment => assignment.WorkerId)
            .HasConstraintName("fk_worker_farm_assignments_worker")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(assignment => assignment.Farm)
            .WithMany(farm => farm.WorkerAssignments)
            .HasForeignKey(assignment => assignment.FarmId)
            .HasConstraintName("fk_worker_farm_assignments_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.AssignedFrom)
            .HasColumnName("assigned_from")
            .IsRequired();

        builder.Property(assignment => assignment.AssignedTo)
            .HasColumnName("assigned_to");

        builder.Property(assignment => assignment.Notes)
            .HasColumnName("notes");

        builder.Property(assignment => assignment.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(assignment => assignment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(assignment => assignment.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(assignment => assignment.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(assignment => assignment.UpdatedBy)
            .HasColumnName("updated_by");

        builder.HasIndex(assignment => assignment.OrganizationId)
            .HasDatabaseName("ix_worker_farm_assignments_organization_id");

        builder.HasIndex(assignment => new { assignment.OrganizationId, assignment.WorkerId })
            .HasDatabaseName("ix_worker_farm_assignments_org_worker");

        builder.HasIndex(assignment => new { assignment.OrganizationId, assignment.FarmId })
            .HasDatabaseName("ix_worker_farm_assignments_org_farm");

        builder.HasIndex(assignment => new { assignment.OrganizationId, assignment.WorkerId, assignment.IsActive })
            .HasDatabaseName("ix_worker_farm_assignments_org_worker_active");

        builder.HasIndex(assignment => new { assignment.OrganizationId, assignment.FarmId, assignment.IsActive })
            .HasDatabaseName("ix_worker_farm_assignments_org_farm_active");
    }
}
