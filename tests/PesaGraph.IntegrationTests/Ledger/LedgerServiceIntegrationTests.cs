using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Ledger.Services;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.IntegrationTests.Ledger;

public class LedgerServiceIntegrationTests : IAsyncLifetime
{
    private readonly InMemoryLedgerRepository _repository;
    private readonly LedgerService _ledgerService;
    private static readonly Guid TenantId = Guid.NewGuid();

    public LedgerServiceIntegrationTests()
    {
        _repository = new InMemoryLedgerRepository();
        _ledgerService = new LedgerService(_repository);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateAccount_AndPostTransaction_ShouldPersistAndCalculateBalance()
    {
        var createResult = await _ledgerService.CreateAccountAsync(
            TenantId,
            "Test Till",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "12345");

        createResult.IsSuccess.Should().BeTrue();
        var accountId = createResult.Value.Id;

        var postRequest = new PostTransactionRequest(
            TenantId,
            "12345",
            Guid.NewGuid(),
            "REF-001",
            5000m,
            50m,
            PaymentRail.Mpesa,
            TransactionType.CustomerToBusiness,
            "John Doe",
            "Payment for services");

        var postResult = await _ledgerService.PostTransactionAsync(postRequest);
        postResult.IsSuccess.Should().BeTrue();
        postResult.Value.Amount.Amount.Should().Be(5000);

        var getResult = await _ledgerService.GetAccountEntriesAsync(accountId);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().HaveCount(1);
        getResult.Value.First().Amount.Amount.Should().Be(5000);
    }

    [Fact]
    public async Task MultipleTransactions_ShouldAccumulateBalance()
    {
        await _ledgerService.CreateAccountAsync(
            TenantId,
            "Multi-Trans Account",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "67890");

        var request1 = new PostTransactionRequest(
            TenantId, "67890", Guid.NewGuid(), "REF-001",
            1000m, 10m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness,
            "User1", "First transaction");

        var request2 = new PostTransactionRequest(
            TenantId, "67890", Guid.NewGuid(), "REF-002",
            2000m, 20m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness,
            "User2", "Second transaction");

        await _ledgerService.PostTransactionAsync(request1);
        await _ledgerService.PostTransactionAsync(request2);

        var accounts = await _ledgerService.GetAccountsAsync(TenantId);
        accounts.IsSuccess.Should().BeTrue();
        accounts.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostTransaction_WithNonExistentAccount_ShouldAutoCreateAccount()
    {
        var request = new PostTransactionRequest(
            TenantId,
            "NEW-ACC-001",
            Guid.NewGuid(),
            "REF-AUTO",
            500m,
            5m,
            PaymentRail.AirtelMoney,
            TransactionType.CustomerToBusiness,
            "Test User",
            "Auto-created account transaction");

        var result = await _ledgerService.PostTransactionAsync(request);

        result.IsSuccess.Should().BeTrue();
        var accounts = await _ledgerService.GetAccountsAsync(TenantId);
        accounts.Value.Should().Contain(a => a.AccountNumber == "NEW-ACC-001");
    }

    [Fact]
    public async Task GetAccounts_ShouldReturnAllTenantAccounts()
    {
        var createResult1 = await _ledgerService.CreateAccountAsync(
            TenantId, "Account 1", AccountType.MpesaTill, PaymentRail.Mpesa, "ACC-001");

        var createResult2 = await _ledgerService.CreateAccountAsync(
            TenantId, "Account 2", AccountType.AirtelMoney, PaymentRail.AirtelMoney, "ACC-002");

        var accountsResult = await _ledgerService.GetAccountsAsync(TenantId);

        accountsResult.IsSuccess.Should().BeTrue();
        accountsResult.Value.Should().HaveCount(2);
        accountsResult.Value.Should().Contain(a => a.Name == "Account 1");
        accountsResult.Value.Should().Contain(a => a.Name == "Account 2");
    }

    [Fact]
    public async Task CreateDuplicateAccount_ShouldReturnConflictError()
    {
        await _ledgerService.CreateAccountAsync(
            TenantId,
            "Duplicate Test",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "DUP-001");

        var duplicateResult = await _ledgerService.CreateAccountAsync(
            TenantId,
            "Duplicate Test 2",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "DUP-001");

        duplicateResult.IsFailure.Should().BeTrue();
        duplicateResult.Error.Code.Should().Be("Account.Exists");
    }

    private class InMemoryLedgerRepository : ILedgerRepository
    {
        private readonly Dictionary<string, Account> _accounts = new();
        private readonly Dictionary<Guid, List<LedgerEntry>> _entries = new();

        public Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts[account.AccountNumber] = account;
            _entries[account.Id] = new List<LedgerEntry>();
            return Task.CompletedTask;
        }

        public Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
        {
            if (_entries.TryGetValue(entry.AccountId, out var entries))
            {
                entries.Add(entry);
            }
            return Task.CompletedTask;
        }

        public Task<Account?> GetAccountByNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = _accounts.Values.FirstOrDefault(a => a.AccountNumber == accountNumber && a.TenantId == tenantId);
            return Task.FromResult(account);
        }

        public Task<Account?> GetAccountByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            var account = _accounts.Values.FirstOrDefault(a => a.Id == accountId);
            return Task.FromResult(account);
        }

        public Task<IReadOnlyList<Account>> ListAccountsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var accounts = _accounts.Values.Where(a => a.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<Account>>(accounts);
        }

        public Task<IReadOnlyList<LedgerEntry>> ListEntriesByAccountAsync(Guid accountId, int limit = 100, CancellationToken cancellationToken = default)
        {
            if (_entries.TryGetValue(accountId, out var entries))
            {
                return Task.FromResult<IReadOnlyList<LedgerEntry>>(entries.TakeLast(limit).ToList());
            }
            return Task.FromResult<IReadOnlyList<LedgerEntry>>(new List<LedgerEntry>());
        }

        public Task UpdateAccountAsync(Account account, CancellationToken cancellationToken = default)
        {
            _accounts[account.AccountNumber] = account;
            return Task.CompletedTask;
        }
    }
}
