using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FarmAreaConfiguration : IEntityTypeConfiguration<FarmArea>
{
    public void Configure(EntityTypeBuilder<FarmArea> builder)
    {
        builder.ToTable("farm_areas");

        builder.HasKey(area => area.Id);

        builder.Property(area => area.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(area => area.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(area => area.Organization)
            .WithMany()
            .HasForeignKey(area => area.OrganizationId)
            .HasConstraintName("fk_farm_area_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(area => area.FarmId)
            .HasColumnName("farm_id")
            .IsRequired();

        builder.HasOne(area => area.Farm)
            .WithMany()
            .HasForeignKey(area => area.FarmId)
            .HasConstraintName("fk_farm_area_farm")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(area => area.ParentFarmAreaId)
            .HasColumnName("parent_farm_area_id");

        builder.HasOne(area => area.ParentFarmArea)
            .WithMany()
            .HasForeignKey(area => area.ParentFarmAreaId)
            .HasConstraintName("fk_farm_area_parent")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(area => area.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(area => area.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(area => area.Description)
            .HasColumnName("description");

        builder.Property(area => area.TotalArea)
            .HasColumnName("total_area")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(area => area.AreaUnitId)
            .HasColumnName("area_unit_id")
            .IsRequired();

        builder.HasOne(area => area.AreaUnit)
            .WithMany()
            .HasForeignKey(area => area.AreaUnitId)
            .HasConstraintName("fk_farm_area_unit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(area => area.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(area => area.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(area => area.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(area => area.UpdatedAt).HasColumnName("updated_at");
        builder.Property(area => area.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(area => new { area.FarmId, area.Code })
            .HasDatabaseName("ux_farm_area_farm_code")
            .IsUnique();

        builder.HasIndex(area => area.ParentFarmAreaId)
            .HasDatabaseName("ix_farm_areas_parent_id");
    }
}
