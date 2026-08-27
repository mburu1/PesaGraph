using System;
using System.Collections.Generic;
using PesaGraph.Shared.Domain;

namespace PesaGraph.Tenancy.Domain;

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    PendingSetup = 3
}

public class Tenant : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; } = TenantStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private readonly List<TenantApiKey> _apiKeys = [];
    public IReadOnlyCollection<TenantApiKey> ApiKeys => _apiKeys.AsReadOnly();

    private readonly List<TenantCredential> _credentials = [];
    public IReadOnlyCollection<TenantCredential> Credentials => _credentials.AsReadOnly();

    private Tenant()
    {
    }

    public Tenant(Guid id, string name, string code, string contactEmail, string contactPhone) : base(id)
    {
        SetName(name);
        SetCode(code);
        ContactEmail = contactEmail.Trim().ToLowerInvariant();
        ContactPhone = contactPhone.Trim();
        Status = TenantStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Tenant Create(string name, string code, string contactEmail, string contactPhone)
    {
        return new Tenant(Guid.NewGuid(), name, code, contactEmail, contactPhone);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tenant code cannot be empty.", nameof(code));
        Code = code.Trim().ToUpperInvariant();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateContactInfo(string email, string phone)
    {
        ContactEmail = email.Trim().ToLowerInvariant();
        ContactPhone = phone.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(TenantStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public TenantApiKey GenerateApiKey(string name, string rawKeyHash, string keyPrefix, DateTimeOffset? expiresAtUtc = null)
    {
        var apiKey = new TenantApiKey(Guid.NewGuid(), Id, name, rawKeyHash, keyPrefix, expiresAtUtc);
        _apiKeys.Add(apiKey);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return apiKey;
    }

    public void RevokeApiKey(Guid apiKeyId)
    {
        var key = _apiKeys.Find(k => k.Id == apiKeyId);
        if (key != null)
        {
            key.Revoke();
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public void SetCredential(string provider, string encryptedJsonPayload)
    {
        var existing = _credentials.Find(c => c.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.UpdatePayload(encryptedJsonPayload);
        }
        else
        {
            _credentials.Add(new TenantCredential(Guid.NewGuid(), Id, provider, encryptedJsonPayload));
        }
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
