using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Tenancy.Domain;

namespace PesaGraph.Tenancy.Repositories;

public class InMemoryTenantRepository : ITenantRepository
{
    private readonly ConcurrentDictionary<Guid, Tenant> _tenants = new();

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tenants.TryGetValue(id, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var tenant = _tenants.Values.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetByApiKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        var tenant = _tenants.Values.FirstOrDefault(t => t.ApiKeys.Any(k => k.KeyHash == keyHash && k.IsValid()));
        return Task.FromResult(tenant);
    }

    public Task<IReadOnlyList<Tenant>> ListAsync(TenantStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _tenants.Values.AsEnumerable();
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return Task.FromResult<IReadOnlyList<Tenant>>(query.ToList());
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants.TryAdd(tenant.Id, tenant);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants[tenant.Id] = tenant;
        return Task.CompletedTask;
    }
}
