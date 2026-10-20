namespace FarmManagement.Domain.Entities;

public sealed class FertilizerReferenceSequence
{
    private FertilizerReferenceSequence()
    {
    }

    public FertilizerReferenceSequence(Guid organizationId, int year, int initialNumber = 0)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (year < 2000 || year > 2200)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be valid.");
        }

        if (initialNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialNumber), "Sequence number cannot be negative.");
        }

        OrganizationId = organizationId;
        Year = year;
        LastNumber = initialNumber;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid OrganizationId { get; private set; }
    public int Year { get; private set; }
    public int LastNumber { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Organization? Organization { get; private set; }

    public int NextNumber(DateTimeOffset now)
    {
        LastNumber++;
        UpdatedAt = now;
        return LastNumber;
    }
}
