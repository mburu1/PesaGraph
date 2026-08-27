using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ledger.Domain;

namespace PesaGraph.Ledger.Repositories;

public interface ILedgerRepository
{
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> ListAccountsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAccountAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAccountAsync(Account account, CancellationToken cancellationToken = default);
    Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerEntry>> ListEntriesByAccountAsync(Guid accountId, int limit = 100, CancellationToken cancellationToken = default);
}

public class InMemoryLedgerRepository : ILedgerRepository
{
    private readonly ConcurrentDictionary<Guid, Account> _accounts = new();
    private readonly ConcurrentBag<LedgerEntry> _entries = [];

    public Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _accounts.TryGetValue(id, out var acc);
        return Task.FromResult(acc);
    }

    public Task<Account?> GetAccountByNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
    {
        var acc = _accounts.Values.FirstOrDefault(a => a.TenantId == tenantId && a.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(acc);
    }

    public Task<IReadOnlyList<Account>> ListAccountsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = _accounts.Values.Where(a => a.TenantId == tenantId).ToList();
        return Task.FromResult<IReadOnlyList<Account>>(list);
    }

    public Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts.TryAdd(account.Id, account);
        return Task.CompletedTask;
    }

    public Task UpdateAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task AddEntryAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LedgerEntry>> ListEntriesByAccountAsync(Guid accountId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = _entries
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.BookedAtUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<LedgerEntry>>(list);
    }
}
