using System;
using System.Collections.Generic;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Ledger.Domain;

public enum AccountType
{
    MpesaTill = 1,
    MpesaPaybill = 2,
    AirtelMoney = 3,
    BankAccount = 4,
    CashFloat = 5
}

public class Account : AggregateRoot<Guid>, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public PaymentRail Rail { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty; // Till no, Paybill no, Bank Acc no
    public Money CurrentBalance { get; private set; } = Money.Zero();
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Account()
    {
    }

    public Account(Guid id, Guid tenantId, string name, AccountType type, PaymentRail rail, string accountNumber, string currency = "KES") : base(id)
    {
        TenantId = tenantId;
        Name = name.Trim();
        Type = type;
        Rail = rail;
        AccountNumber = accountNumber.Trim();
        CurrentBalance = Money.Zero(currency);
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static Account Create(Guid tenantId, string name, AccountType type, PaymentRail rail, string accountNumber, string currency = "KES")
    {
        return new Account(Guid.NewGuid(), tenantId, name, type, rail, accountNumber, currency);
    }

    public void Credit(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Credit amount cannot be negative.", nameof(amount));
        CurrentBalance = new Money(CurrentBalance.Amount + amount, CurrentBalance.Currency);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Debit amount cannot be negative.", nameof(amount));
        CurrentBalance = new Money(CurrentBalance.Amount - amount, CurrentBalance.Currency);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
