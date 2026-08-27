using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ingestion.Domain;

namespace PesaGraph.Ingestion.Repositories;

public interface IRawWebhookRepository
{
    Task<RawWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RawWebhookEvent?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RawWebhookEvent>> ListByTenantAsync(Guid tenantId, int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(RawWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(RawWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
