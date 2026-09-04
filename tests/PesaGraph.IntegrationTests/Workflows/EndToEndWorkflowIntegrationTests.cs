using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Audit.Repositories;
using PesaGraph.Audit.Services;
using PesaGraph.Ledger.Domain;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Ledger.Services;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.IntegrationTests.Workflows;

public class EndToEndWorkflowIntegrationTests : IAsyncLifetime
{
    private readonly LedgerService _ledgerService;
    private readonly AuditService _auditService;
    private static readonly Guid TenantId = Guid.NewGuid();

    public EndToEndWorkflowIntegrationTests()
    {
        var ledgerRepo = new InMemoryLedgerRepository();
        var auditRepo = new InMemoryAuditRepository();
        _ledgerService = new LedgerService(ledgerRepo);
        _auditService = new AuditService(auditRepo);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CompleteTransactionWorkflow_ShouldPersistAndAudit()
    {
        var operator1Email = "operator1@pesagraph.com";

        var createAccountResult = await _ledgerService.CreateAccountAsync(
            TenantId,
            "Primary Till",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "TILL-001");

        createAccountResult.IsSuccess.Should().BeTrue();

        await _auditService.RecordAuditAsync(
            TenantId,
            operator1Email,
            "AccountCreated",
            "Account",
            createAccountResult.Value.Id.ToString(),
            "Created primary till account");

        var transactionRequest = new PostTransactionRequest(
            TenantId,
            "TILL-001",
            Guid.NewGuid(),
            "TXN-20240128-001",
            50000m,
            500m,
            PaymentRail.Mpesa,
            TransactionType.CustomerToBusiness,
            "Jane Doe",
            "Large payment deposit");

        var postResult = await _ledgerService.PostTransactionAsync(transactionRequest);
        postResult.IsSuccess.Should().BeTrue();

        await _auditService.RecordAuditAsync(
            TenantId,
            operator1Email,
            "TransactionPosted",
            "Transaction",
            postResult.Value.Id.ToString(),
            $"Posted transaction of {postResult.Value.Amount} KES");

        var auditLogsResult = await _auditService.GetAuditLogsAsync(TenantId);
        auditLogsResult.IsSuccess.Should().BeTrue();
        auditLogsResult.Value.Should().HaveCountGreaterThanOrEqualTo(2);
        auditLogsResult.Value.Should().Contain(log => log.Actor == operator1Email && log.Action == "AccountCreated");
        auditLogsResult.Value.Should().Contain(log => log.Actor == operator1Email && log.Action == "TransactionPosted");
    }

    [Fact]
    public async Task MultipleOperators_DifferentActivities_ShouldAuditAll()
    {
        var operator1 = "supervisor@pesagraph.com";
        var operator2 = "cashier@pesagraph.com";

        var accountResult = await _ledgerService.CreateAccountAsync(
            TenantId,
            "Shared Account",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "SHARED-001");

        await _auditService.RecordAuditAsync(
            TenantId,
            operator1,
            "AccountCreated",
            "Account",
            accountResult.Value.Id.ToString(),
            "Created by supervisor");

        var txn1 = new PostTransactionRequest(
            TenantId, "SHARED-001", Guid.NewGuid(), "TXN-001",
            10000m, 100m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness,
            "Customer A", "Payment 1");

        await _ledgerService.PostTransactionAsync(txn1);

        await _auditService.RecordAuditAsync(
            TenantId,
            operator2,
            "TransactionPosted",
            "Transaction",
            "TXN-001",
            "Cashier posted transaction");

        var txn2 = new PostTransactionRequest(
            TenantId, "SHARED-001", Guid.NewGuid(), "TXN-002",
            20000m, 200m, PaymentRail.Mpesa, TransactionType.CustomerToBusiness,
            "Customer B", "Payment 2");

        await _ledgerService.PostTransactionAsync(txn2);

        await _auditService.RecordAuditAsync(
            TenantId,
            operator2,
            "TransactionPosted",
            "Transaction",
            "TXN-002",
            "Cashier posted second transaction");

        var auditLogs = await _auditService.GetAuditLogsAsync(TenantId);
        auditLogs.Value.Count(log => log.Actor == operator2).Should().Be(2);
        auditLogs.Value.Count(log => log.Actor == operator1).Should().Be(1);
    }

    [Fact]
    public async Task TransactionReversal_ShouldCreateAuditTrail()
    {
        var operatorEmail = "reversal@pesagraph.com";

        await _ledgerService.CreateAccountAsync(
            TenantId,
            "Reversal Test Account",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "REV-001");

        var txnRequest = new PostTransactionRequest(
            TenantId,
            "REV-001",
            Guid.NewGuid(),
            "REV-TXN-001",
            5000m,
            50m,
            PaymentRail.Mpesa,
            TransactionType.CustomerToBusiness,
            "Test Customer",
            "Test transaction for reversal");

        var txnResult = await _ledgerService.PostTransactionAsync(txnRequest);

        await _auditService.RecordAuditAsync(
            TenantId,
            operatorEmail,
            "TransactionPosted",
            "Transaction",
            txnResult.Value.Id.ToString(),
            "Posted transaction for reversal test");

        await _auditService.RecordAuditAsync(
            TenantId,
            operatorEmail,
            "TransactionReversed",
            "Transaction",
            txnResult.Value.Id.ToString(),
            "Reversed due to duplicate entry");

        var auditLogs = await _auditService.GetAuditLogsAsync(TenantId);
        var transactionLogs = auditLogs.Value.Where(log => log.ResourceId == txnResult.Value.Id.ToString()).ToList();

        transactionLogs.Should().HaveCount(2);
        // Audit logs returned most recent first
        transactionLogs[0].Action.Should().Be("TransactionReversed");
        transactionLogs[1].Action.Should().Be("TransactionPosted");
    }

    [Fact]
    public async Task DailyReconciliation_ShouldShowCompleteAuditTrail()
    {
        var reconciliationDate = DateTime.UtcNow.Date;

        var accountResult = await _ledgerService.CreateAccountAsync(
            TenantId,
            "Daily Reconciliation Account",
            AccountType.MpesaTill,
            PaymentRail.Mpesa,
            "REC-DAILY");

        var transactions = new[] { 1000m, 2500m, 3200m, 1800m, 2100m };
        var totalExpected = transactions.Sum();

        foreach (var (amount, index) in transactions.Select((a, i) => (a, i)))
        {
            var request = new PostTransactionRequest(
                TenantId,
                "REC-DAILY",
                Guid.NewGuid(),
                $"REC-TXN-{index}",
                amount,
                amount * 0.01m,
                PaymentRail.Mpesa,
                TransactionType.CustomerToBusiness,
                $"Customer {index}",
                $"Transaction {index} on {reconciliationDate}");

            await _ledgerService.PostTransactionAsync(request);
        }

        await _auditService.RecordAuditAsync(
            TenantId,
            "reconciler@pesagraph.com",
            "DailyReconciliation",
            "Reconciliation",
            "REC-DAILY",
            $"Daily reconciliation completed. Total processed: {totalExpected} KES");

        var accounts = await _ledgerService.GetAccountsAsync(TenantId);
        accounts.IsSuccess.Should().BeTrue();

        var auditLogs = await _auditService.GetAuditLogsAsync(TenantId);
        auditLogs.Value.Should().Contain(log => log.Action == "DailyReconciliation");
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

    private class InMemoryAuditRepository : IAuditRepository
    {
        private readonly List<PesaGraph.Audit.Domain.AuditLog> _logs = new();

        public Task AddLogAsync(PesaGraph.Audit.Domain.AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _logs.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PesaGraph.Audit.Domain.AuditLog>> ListLogsByTenantAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PesaGraph.Audit.Domain.AuditLog>>(_logs
                .Where(l => l.TenantId == tenantId)
                .OrderByDescending(l => l.TimestampUtc)
                .Take(limit)
                .ToList());
        }
    }
}
