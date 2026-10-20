using FarmManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmManagement.Infrastructure.Persistence.Configurations;

public sealed class FertilizerReferenceSequenceConfiguration : IEntityTypeConfiguration<FertilizerReferenceSequence>
{
    public void Configure(EntityTypeBuilder<FertilizerReferenceSequence> builder)
    {
        builder.ToTable("fertilizer_reference_sequences");

        builder.HasKey(s => new { s.OrganizationId, s.Year });

        builder.Property(s => s.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .HasConstraintName("fk_fertilizer_reference_sequences_organization")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(s => s.LastNumber)
            .HasColumnName("last_number")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
