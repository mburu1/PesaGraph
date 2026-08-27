using System;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Reconciliation.Domain;

public enum MatchConfidence
{
    Exact = 1,
    High = 2,
    Medium = 3,
    Low = 4,
    Manual = 5
}

public class MatchedPair : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid SourceTransactionId { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;
    public PaymentRail SourceRail { get; private set; }
    public Guid TargetTransactionId { get; private set; }
    public string TargetReference { get; private set; } = string.Empty;
    public PaymentRail TargetRail { get; private set; }
    public decimal Amount { get; private set; }
    public MatchConfidence Confidence { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public DateTimeOffset MatchedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? ResolvedBy { get; private set; }

    private MatchedPair()
    {
    }

    public MatchedPair(
        Guid id,
        Guid tenantId,
        Guid sourceTransactionId,
        string sourceReference,
        PaymentRail sourceRail,
        Guid targetTransactionId,
        string targetReference,
        PaymentRail targetRail,
        decimal amount,
        MatchConfidence confidence,
        string ruleName,
        string? resolvedBy = null) : base(id)
    {
        TenantId = tenantId;
        SourceTransactionId = sourceTransactionId;
        SourceReference = sourceReference;
        SourceRail = sourceRail;
        TargetTransactionId = targetTransactionId;
        TargetReference = targetReference;
        TargetRail = targetRail;
        Amount = amount;
        Confidence = confidence;
        RuleName = ruleName;
        MatchedAtUtc = DateTimeOffset.UtcNow;
        ResolvedBy = resolvedBy;
    }
}

public class UnmatchedItem : AggregateRoot<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid TransactionId { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public PaymentRail Rail { get; private set; }
    public decimal Amount { get; private set; }
    public string Counterparty { get; private set; } = string.Empty;
    public MatchStatus Status { get; private set; } = MatchStatus.Unmatched;
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private UnmatchedItem()
    {
    }

    public UnmatchedItem(
        Guid id,
        Guid tenantId,
        Guid transactionId,
        string externalReference,
        PaymentRail rail,
        decimal amount,
        string counterparty) : base(id)
    {
        TenantId = tenantId;
        TransactionId = transactionId;
        ExternalReference = externalReference;
        Rail = rail;
        Amount = amount;
        Counterparty = counterparty;
        Status = MatchStatus.Unmatched;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static UnmatchedItem Create(Guid tenantId, Guid transactionId, string externalReference, PaymentRail rail, decimal amount, string counterparty) =>
        new(Guid.NewGuid(), tenantId, transactionId, externalReference, rail, amount, counterparty);

    public void ResolveManually(string resolvedBy, string notes)
    {
        Status = MatchStatus.ManuallyResolved;
        ResolutionNotes = $"{notes} (Resolved by: {resolvedBy})";
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }
}
