using System;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Ingestion.Domain;

public enum IngestionStatus
{
    Received = 1,
    Normalised = 2,
    Duplicate = 3,
    Failed = 4
}

public class RawWebhookEvent : AggregateRoot<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public PaymentRail Rail { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string ExternalReference { get; private set; } = string.Empty;
    public string RawJson { get; private set; } = string.Empty;
    public string? HeaderJson { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public IngestionStatus Status { get; private set; } = IngestionStatus.Received;
    public string? FailureReason { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private RawWebhookEvent()
    {
    }

    public RawWebhookEvent(
        Guid id,
        Guid tenantId,
        PaymentRail rail,
        string eventType,
        string externalReference,
        string rawJson,
        string? headerJson,
        string idempotencyKey) : base(id)
    {
        TenantId = tenantId;
        Rail = rail;
        EventType = eventType;
        ExternalReference = externalReference;
        RawJson = rawJson;
        HeaderJson = headerJson;
        IdempotencyKey = idempotencyKey;
        Status = IngestionStatus.Received;
        ReceivedAtUtc = DateTimeOffset.UtcNow;
    }

    public static RawWebhookEvent Create(
        Guid tenantId,
        PaymentRail rail,
        string eventType,
        string externalReference,
        string rawJson,
        string? headerJson,
        string idempotencyKey)
    {
        return new RawWebhookEvent(
            Guid.NewGuid(),
            tenantId,
            rail,
            eventType,
            externalReference,
            rawJson,
            headerJson,
            idempotencyKey);
    }

    public void MarkNormalised()
    {
        Status = IngestionStatus.Normalised;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkDuplicate()
    {
        Status = IngestionStatus.Duplicate;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = IngestionStatus.Failed;
        FailureReason = reason;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }
}
