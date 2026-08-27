using System;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Audit.Domain;

public class AuditLog : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public string Actor { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTimeOffset TimestampUtc { get; private set; } = DateTimeOffset.UtcNow;

    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid tenantId,
        string actor,
        string action,
        string resourceType,
        string resourceId,
        string? details) : base(id)
    {
        TenantId = tenantId;
        Actor = actor;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Details = details;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    public static AuditLog Create(Guid tenantId, string actor, string action, string resourceType, string resourceId, string? details = null) =>
        new(Guid.NewGuid(), tenantId, actor, action, resourceType, resourceId, details);
}
