using System;
using FluentAssertions;
using PesaGraph.Ledger.Domain;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Ledger;

public class AccountTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static Account CreateAccount(
        string name = "M-Pesa Till",
        AccountType type = AccountType.MpesaTill,
        PaymentRail rail = PaymentRail.Mpesa,
        string accountNumber = "12345678",
        string currency = "KES")
    {
        return Account.Create(TenantId, name, type, rail, accountNumber, currency);
    }

    [Fact]
    public void Create_ShouldReturnAccountWithExpectedProperties()
    {
        var account = CreateAccount();

        account.Id.Should().NotBe(Guid.Empty);
        account.TenantId.Should().Be(TenantId);
        account.Name.Should().Be("M-Pesa Till");
        account.Type.Should().Be(AccountType.MpesaTill);
        account.Rail.Should().Be(PaymentRail.Mpesa);
        account.AccountNumber.Should().Be("12345678");
        account.IsActive.Should().BeTrue();
        account.CurrentBalance.Amount.Should().Be(0m);
        account.CurrentBalance.Currency.Should().Be("KES");
    }

    [Fact]
    public void Create_ShouldStartWithZeroBalance()
    {
        var account = CreateAccount();

        account.CurrentBalance.Should().Be(Money.Zero("KES"));
    }

    [Fact]
    public void Credit_WithPositiveAmount_ShouldIncreaseBalance()
    {
        var account = CreateAccount();

        account.Credit(5000m);

        account.CurrentBalance.Amount.Should().Be(5000m);
    }

    [Fact]
    public void Credit_MultipleTimes_ShouldAccumulate()
    {
        var account = CreateAccount();

        account.Credit(3000m);
        account.Credit(2000m);

        account.CurrentBalance.Amount.Should().Be(5000m);
    }

    [Fact]
    public void Debit_WithPositiveAmount_ShouldDecreaseBalance()
    {
        var account = CreateAccount();
        account.Credit(10000m);

        account.Debit(3000m);

        account.CurrentBalance.Amount.Should().Be(7000m);
    }

    [Fact]
    public void Credit_WithNegativeAmount_ShouldThrowArgumentException()
    {
        var account = CreateAccount();

        var act = () => account.Credit(-100m);

        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Fact]
    public void Debit_WithNegativeAmount_ShouldThrowArgumentException()
    {
        var account = CreateAccount();

        var act = () => account.Debit(-100m);

        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Fact]
    public void Credit_ShouldUpdateUpdatedAtUtc()
    {
        var account = CreateAccount();

        account.Credit(1m);

        account.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Debit_ShouldUpdateUpdatedAtUtc()
    {
        var account = CreateAccount();
        account.Credit(100m);

        account.Debit(50m);

        account.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AccountNumber_ShouldBeTrimmed()
    {
        var account = Account.Create(TenantId, "Bank", AccountType.BankAccount, PaymentRail.Bank, "  ACC-001  ");

        account.AccountNumber.Should().Be("ACC-001");
    }
}
