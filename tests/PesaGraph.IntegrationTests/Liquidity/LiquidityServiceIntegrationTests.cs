using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Ledger.Services;
using PesaGraph.Liquidity.DTOs;
using PesaGraph.Liquidity.Services;
using PesaGraph.Shared.Domain.ValueObjects;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.IntegrationTests.Liquidity;

public class LiquidityServiceIntegrationTests : IAsyncLifetime
{
    private readonly InMemoryLedgerRepository _ledgerRepository;
    private readonly LedgerService _ledgerService;
    private readonly LiquidityService _liquidityService;
    private static readonly Guid TenantId = Guid.NewGuid();

    public LiquidityServiceIntegrationTests()
    {
        _ledgerRepository = new InMemoryLedgerRepository();
        _ledgerService = new LedgerService(_ledgerRepository);
        _liquidityService = new LiquidityService(_ledgerService);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetFloatCockpit_WithNoAccounts_ShouldReturnEmptySummary()
    {
        var result = await _liquidityService.GetFloatCockpitAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalLiquidFloat.Should().Be(0);
        result.Value.Accounts.Should().BeEmpty();
        result.Value.ActiveAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFloatCockpit_WithMpesaAccount_ShouldReturnCorrectTotals()
    {
        await _ledgerService.CreateAccountAsync(TenantId, "M-Pesa Till", AccountType.MpesaTill, PaymentRail.Mpesa, "TILL-001");
        var postRequest = new PostTransactionRequest(TenantId, "TILL-001", Guid.NewGuid(), "REF-001", 10000m, 100m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness, "Customer A", "Deposit");
        await _ledgerService.PostTransactionAsync(postRequest);

        var result = await _liquidityService.GetFloatCockpitAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalLiquidFloat.Should().Be(9900m); // 10000 - 100 fee
        result.Value.MpesaFloat.Should().Be(9900m);
    }

    [Fact]
    public async Task GetFloatCockpit_WithMultipleRails_ShouldSeparateByRail()
    {
        await _ledgerService.CreateAccountAsync(TenantId, "M-Pesa Account", AccountType.MpesaPaybill, PaymentRail.Mpesa, "MPESA-001");
        await _ledgerService.CreateAccountAsync(TenantId, "Airtel Account", AccountType.AirtelMoney, PaymentRail.AirtelMoney, "AIRTEL-001");

        await _ledgerService.PostTransactionAsync(new PostTransactionRequest(TenantId, "MPESA-001", Guid.NewGuid(), "REF-001", 5000m, 50m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness, "Customer A", "M-Pesa Deposit"));
        await _ledgerService.PostTransactionAsync(new PostTransactionRequest(TenantId, "AIRTEL-001", Guid.NewGuid(), "REF-002", 3000m, 30m, PaymentRail.AirtelMoney, TransactionType.CustomerToBusiness, "Customer B", "Airtel Deposit"));

        var result = await _liquidityService.GetFloatCockpitAsync(TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.MpesaFloat.Should().Be(4950m); // 5000 - 50 fee
        result.Value.AirtelFloat.Should().Be(2970m); // 3000 - 30 fee
        result.Value.TotalLiquidFloat.Should().Be(7920m);
    }

    [Fact]
    public async Task GetFloatCockpit_WithLowFloatThreshold_ShouldGenerateAlerts()
    {
        await _ledgerService.CreateAccountAsync(TenantId, "Low Float Account", AccountType.MpesaTill, PaymentRail.Mpesa, "LOW-001");
        await _ledgerService.PostTransactionAsync(new PostTransactionRequest(TenantId, "LOW-001", Guid.NewGuid(), "REF-001", 1000m, 10m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness, "Customer A", "Small Deposit"));

        var result = await _liquidityService.GetFloatCockpitAsync(TenantId, lowFloatThreshold: 50000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveAlerts.Should().HaveCount(1);
        result.Value.ActiveAlerts[0].Severity.Should().Be(FloatAlertSeverity.Warning);
    }

    [Fact]
    public async Task GetFloatCockpit_WithCriticalBalance_ShouldGenerateCriticalAlert()
    {
        await _ledgerService.CreateAccountAsync(TenantId, "Zero Float Account", AccountType.MpesaTill, PaymentRail.Mpesa, "ZERO-001");
        // No transactions = zero balance

        var result = await _liquidityService.GetFloatCockpitAsync(TenantId, lowFloatThreshold: 10000m);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveAlerts.Should().HaveCount(1);
        result.Value.ActiveAlerts[0].Severity.Should().Be(FloatAlertSeverity.Critical);
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
                // Note: Account balance is already updated by LedgerService before calling AddEntryAsync
                // No need to update again here
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
