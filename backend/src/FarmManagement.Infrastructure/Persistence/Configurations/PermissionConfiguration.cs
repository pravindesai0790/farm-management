using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(permission => permission.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(permission => permission.Name)
            .HasDatabaseName("ux_permissions_name")
            .IsUnique();

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(permission => permission.Module)
            .HasColumnName("module")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(permission => permission.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
