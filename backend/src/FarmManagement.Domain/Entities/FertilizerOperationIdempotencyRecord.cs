namespace FarmManagement.Domain.Entities;

public sealed class FertilizerOperationIdempotencyRecord
{
    private FertilizerOperationIdempotencyRecord()
    {
        OperationType = string.Empty;
        IdempotencyKey = string.Empty;
        RequestHash = string.Empty;
    }

    public FertilizerOperationIdempotencyRecord(
        Guid organizationId,
        string operationType,
        string idempotencyKey,
        Guid fertilizerApplicationId,
        string requestHash)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("An organization is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(operationType))
        {
            throw new ArgumentException("An operation type is required.", nameof(operationType));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        }

        if (fertilizerApplicationId == Guid.Empty)
        {
            throw new ArgumentException("A fertilizer application ID is required.", nameof(fertilizerApplicationId));
        }

        if (string.IsNullOrWhiteSpace(requestHash))
        {
            throw new ArgumentException("A request hash is required.", nameof(requestHash));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        OperationType = operationType.Trim();
        IdempotencyKey = idempotencyKey.Trim();
        FertilizerApplicationId = fertilizerApplicationId;
        RequestHash = requestHash.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string OperationType { get; private set; }
    public string IdempotencyKey { get; private set; }
    public Guid FertilizerApplicationId { get; private set; }
    public string RequestHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Organization? Organization { get; private set; }
    public FertilizerApplication? FertilizerApplication { get; private set; }
}
