using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(user => user.Organization)
            .WithMany(organization => organization.Users)
            .HasForeignKey(user => user.OrganizationId)
            .HasConstraintName("fk_users_organization")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(user => user.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .HasDatabaseName("ux_users_email")
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.FailedLoginCount)
            .HasColumnName("failed_login_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(user => user.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(user => user.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
