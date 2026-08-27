using System;
using PesaGraph.Shared.Domain;

namespace PesaGraph.Tenancy.Domain;

public class TenantCredential : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string EncryptedJsonPayload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private TenantCredential()
    {
    }

    internal TenantCredential(Guid id, Guid tenantId, string provider, string encryptedJsonPayload) : base(id)
    {
        TenantId = tenantId;
        Provider = provider.Trim().ToLowerInvariant();
        EncryptedJsonPayload = encryptedJsonPayload;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void UpdatePayload(string encryptedJsonPayload)
    {
        EncryptedJsonPayload = encryptedJsonPayload;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
