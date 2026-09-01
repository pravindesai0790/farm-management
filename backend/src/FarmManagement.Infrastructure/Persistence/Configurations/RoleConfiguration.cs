using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(role => role.IsSystemRole)
            .HasColumnName("is_system_role")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(role => role.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(role => role.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(role => role.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
