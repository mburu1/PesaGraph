using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Shared.Enums;

namespace PesaGraph.Reconciliation.Repositories;

public interface IReconciliationRepository
{
    Task AddMatchedPairAsync(MatchedPair pair, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MatchedPair>> ListMatchedPairsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default);
    Task AddUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default);
    Task<UnmatchedItem?> GetUnmatchedByReferenceAsync(Guid tenantId, string reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnmatchedItem>> ListUnmatchedItemsAsync(Guid tenantId, MatchStatus? status = MatchStatus.Unmatched, CancellationToken cancellationToken = default);
    Task UpdateUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default);
}

public class InMemoryReconciliationRepository : IReconciliationRepository
{
    private readonly ConcurrentBag<MatchedPair> _matchedPairs = [];
    private readonly ConcurrentDictionary<Guid, UnmatchedItem> _unmatchedItems = new();

    public Task AddMatchedPairAsync(MatchedPair pair, CancellationToken cancellationToken = default)
    {
        _matchedPairs.Add(pair);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MatchedPair>> ListMatchedPairsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = _matchedPairs
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.MatchedAtUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<MatchedPair>>(list);
    }

    public Task AddUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default)
    {
        _unmatchedItems.TryAdd(item.Id, item);
        return Task.CompletedTask;
    }

    public Task<UnmatchedItem?> GetUnmatchedByReferenceAsync(Guid tenantId, string reference, CancellationToken cancellationToken = default)
    {
        var item = _unmatchedItems.Values.FirstOrDefault(u => u.TenantId == tenantId && u.ExternalReference.Equals(reference, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<UnmatchedItem>> ListUnmatchedItemsAsync(Guid tenantId, MatchStatus? status = MatchStatus.Unmatched, CancellationToken cancellationToken = default)
    {
        var query = _unmatchedItems.Values.Where(u => u.TenantId == tenantId);
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var list = query.OrderByDescending(u => u.CreatedAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<UnmatchedItem>>(list);
    }

    public Task UpdateUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default)
    {
        _unmatchedItems[item.Id] = item;
        return Task.CompletedTask;
    }
}
