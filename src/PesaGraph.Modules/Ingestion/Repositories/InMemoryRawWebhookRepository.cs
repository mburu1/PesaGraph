using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ingestion.Domain;

namespace PesaGraph.Ingestion.Repositories;

public class InMemoryRawWebhookRepository : IRawWebhookRepository
{
    private readonly ConcurrentDictionary<Guid, RawWebhookEvent> _events = new();

    public Task<RawWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _events.TryGetValue(id, out var ev);
        return Task.FromResult(ev);
    }

    public Task<RawWebhookEvent?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var ev = _events.Values.FirstOrDefault(e => e.TenantId == tenantId && e.IdempotencyKey == idempotencyKey);
        return Task.FromResult(ev);
    }

    public Task<IReadOnlyList<RawWebhookEvent>> ListByTenantAsync(Guid tenantId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var list = _events.Values
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.ReceivedAtUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<RawWebhookEvent>>(list);
    }

    public Task AddAsync(RawWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _events.TryAdd(webhookEvent.Id, webhookEvent);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RawWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _events[webhookEvent.Id] = webhookEvent;
        return Task.CompletedTask;
    }
}
