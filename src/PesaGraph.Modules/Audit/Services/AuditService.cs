using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Audit.Domain;
using PesaGraph.Audit.Repositories;
using PesaGraph.Shared.Results;

namespace PesaGraph.Audit.Services;

public interface IAuditService
{
    Task<Result> RecordAuditAsync(Guid tenantId, string actor, string action, string resourceType, string resourceId, string? details = null, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AuditLog>>> GetAuditLogsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<Result> RecordAuditAsync(Guid tenantId, string actor, string action, string resourceType, string resourceId, string? details = null, CancellationToken cancellationToken = default)
    {
        var log = AuditLog.Create(tenantId, actor, action, resourceType, resourceId, details);
        await _auditRepository.AddLogAsync(log, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AuditLog>>> GetAuditLogsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _auditRepository.ListLogsByTenantAsync(tenantId, limit, cancellationToken);
        return Result.Success(list);
    }
}
