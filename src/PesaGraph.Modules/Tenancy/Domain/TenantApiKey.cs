using System;
using PesaGraph.Shared.Domain;

namespace PesaGraph.Tenancy.Domain;

public class TenantApiKey : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }

    private TenantApiKey()
    {
    }

    internal TenantApiKey(Guid id, Guid tenantId, string name, string keyHash, string keyPrefix, DateTimeOffset? expiresAtUtc) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        KeyHash = keyHash;
        KeyPrefix = keyPrefix;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void RecordUsage()
    {
        LastUsedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsValid()
    {
        if (!IsActive) return false;
        if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value < DateTimeOffset.UtcNow) return false;
        return true;
    }
}
