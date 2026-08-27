using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Audit.Domain;

namespace PesaGraph.Audit.Repositories;

public interface IAuditRepository
{
    Task AddLogAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> ListLogsByTenantAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default);
}

public class InMemoryAuditRepository : IAuditRepository
{
    private readonly ConcurrentBag<AuditLog> _logs = [];

    public Task AddLogAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        _logs.Add(log);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLog>> ListLogsByTenantAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = _logs
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.TimestampUtc)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditLog>>(list);
    }
}
