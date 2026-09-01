using System.Text.Json;

namespace FarmManagement.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
        Action = string.Empty;
    }

    public AuditLog(
        string action,
        Guid? organizationId = null,
        Guid? userId = null,
        string? entityType = null,
        Guid? entityId = null,
        JsonDocument? details = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("An audit action is required.", nameof(action));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        UserId = userId;
        Action = action.Trim();
        EntityType = entityType?.Trim();
        EntityId = entityId;
        Details = details;
        IpAddress = ipAddress?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; }

    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public JsonDocument? Details { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Organization? Organization { get; private set; }

    public User? User { get; private set; }
}
