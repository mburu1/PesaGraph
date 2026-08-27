using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Ledger.Services;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Ledger;

public class LedgerServiceTests
{
    private readonly ILedgerRepository _repository;
    private readonly LedgerService _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public LedgerServiceTests()
    {
        _repository = Substitute.For<ILedgerRepository>();
        _sut = new LedgerService(_repository);
    }

    [Fact]
    public async Task CreateAccountAsync_WhenNoExistingAccount_ShouldSucceed()
    {
        _repository.GetAccountByNumberAsync(TenantId, "12345", Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var result = await _sut.CreateAccountAsync(
            TenantId, "Test Account", AccountType.MpesaTill, PaymentRail.Mpesa, "12345");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccountNumber.Should().Be("12345");
        await _repository.Received(1).AddAccountAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAccountAsync_WhenAccountExists_ShouldReturnConflictError()
    {
        var existing = Account.Create(TenantId, "Existing", AccountType.MpesaTill, PaymentRail.Mpesa, "12345");
        _repository.GetAccountByNumberAsync(TenantId, "12345", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.CreateAccountAsync(
            TenantId, "New Account", AccountType.MpesaTill, PaymentRail.Mpesa, "12345");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Account.Exists");
    }

    [Fact]
    public async Task PostTransactionAsync_WhenAccountExists_ShouldCreditAndCreateEntry()
    {
        var account = Account.Create(TenantId, "Till", AccountType.MpesaTill, PaymentRail.Mpesa, "54321");
        _repository.GetAccountByNumberAsync(TenantId, "54321", Arg.Any<CancellationToken>())
            .Returns(account);

        var request = new PostTransactionRequest(
            TenantId,
            "54321",
            Guid.NewGuid(),
            "EXT-REF-001",
            1000m,
            10m,
            PaymentRail.Mpesa,
            TransactionType.CustomerToBusiness,
            "Jane Doe",
            "Payment received");

        var result = await _sut.PostTransactionAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Direction.Should().Be(EntryDirection.Credit);
        await _repository.Received(1).UpdateAccountAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddEntryAsync(Arg.Any<LedgerEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostTransactionAsync_WhenAccountNotFound_ShouldAutoCreateAccount()
    {
        _repository.GetAccountByNumberAsync(TenantId, "AUTO-ACC", Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var request = new PostTransactionRequest(
            TenantId,
            "AUTO-ACC",
            Guid.NewGuid(),
            "REF-AUTO",
            500m,
            5m,
            PaymentRail.AirtelMoney,
            TransactionType.CustomerToBusiness,
            "Test User",
            "Auto-created account");

        var result = await _sut.PostTransactionAsync(request);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAccountAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccountsAsync_ShouldReturnAccountsFromRepository()
    {
        var accounts = new List<Account>
        {
            Account.Create(TenantId, "Acc1", AccountType.MpesaTill, PaymentRail.Mpesa, "111"),
            Account.Create(TenantId, "Acc2", AccountType.AirtelMoney, PaymentRail.AirtelMoney, "222")
        };
        _repository.ListAccountsByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Account>)accounts);

        var result = await _sut.GetAccountsAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccountEntriesAsync_ShouldReturnEntriesFromRepository()
    {
        var accountId = Guid.NewGuid();
        _repository.ListEntriesByAccountAsync(accountId, 100, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<LedgerEntry>)new List<LedgerEntry>());

        var result = await _sut.GetAccountEntriesAsync(accountId);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).ListEntriesByAccountAsync(accountId, 100, Arg.Any<CancellationToken>());
    }
}
