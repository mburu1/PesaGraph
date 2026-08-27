using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Tenancy.Domain;

namespace PesaGraph.Tenancy.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByApiKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAsync(TenantStatus? status = null, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
