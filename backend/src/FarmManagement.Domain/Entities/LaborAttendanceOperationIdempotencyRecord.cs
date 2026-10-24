namespace FarmManagement.Domain.Entities;

public sealed class LaborAttendanceOperationIdempotencyRecord
{
    private LaborAttendanceOperationIdempotencyRecord()
    {
        OperationType = string.Empty;
        IdempotencyKey = string.Empty;
        RequestHash = string.Empty;
        ResponsePayload = string.Empty;
    }

    public LaborAttendanceOperationIdempotencyRecord(
        Guid organizationId,
        string operationType,
        string idempotencyKey,
        string requestHash,
        string responsePayload,
        Guid createdBy)
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

        if (string.IsNullOrWhiteSpace(requestHash))
        {
            throw new ArgumentException("A request hash is required.", nameof(requestHash));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A created by user is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        OperationType = operationType.Trim();
        IdempotencyKey = idempotencyKey.Trim();
        RequestHash = requestHash.Trim();
        ResponsePayload = responsePayload ?? string.Empty;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string OperationType { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string RequestHash { get; private set; }
    public string ResponsePayload { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Organization? Organization { get; private set; }
}
