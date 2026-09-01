using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(refreshToken => refreshToken.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .HasConstraintName("fk_refresh_tokens_user")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ClientType)
            .HasColumnName("client_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(255);

        builder.Property(refreshToken => refreshToken.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasIndex(refreshToken => refreshToken.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(refreshToken => refreshToken.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id");

        builder.Property(refreshToken => refreshToken.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(100);

        builder.Property(refreshToken => refreshToken.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(100);
    }
}
