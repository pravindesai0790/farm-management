using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(setting => setting.Key)
            .HasColumnName("key")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(setting => setting.Key)
            .IsUnique();

        builder.Property(setting => setting.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(setting => setting.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(setting => setting.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
