using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(organization => organization.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(organization => organization.Code)
            .HasDatabaseName("ux_organizations_code")
            .IsUnique();

        builder.Property(organization => organization.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(organization => organization.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(organization => organization.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
