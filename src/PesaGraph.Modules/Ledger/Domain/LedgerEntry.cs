using System;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Ledger.Domain;

public enum EntryDirection
{
    Debit = 1,
    Credit = 2
}

public class LedgerEntry : Entity<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid AccountId { get; private set; }
    public Guid TransactionId { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public EntryDirection Direction { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public Money FeeAmount { get; private set; } = Money.Zero();
    public Money BalanceAfter { get; private set; } = Money.Zero();
    public string CounterpartyInfo { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset BookedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private LedgerEntry()
    {
    }

    public LedgerEntry(
        Guid id,
        Guid tenantId,
        Guid accountId,
        Guid transactionId,
        string externalReference,
        EntryDirection direction,
        Money amount,
        Money feeAmount,
        Money balanceAfter,
        string counterpartyInfo,
        string description) : base(id)
    {
        TenantId = tenantId;
        AccountId = accountId;
        TransactionId = transactionId;
        ExternalReference = externalReference;
        Direction = direction;
        Amount = amount;
        FeeAmount = feeAmount;
        BalanceAfter = balanceAfter;
        CounterpartyInfo = counterpartyInfo;
        Description = description;
        BookedAtUtc = DateTimeOffset.UtcNow;
    }
}
