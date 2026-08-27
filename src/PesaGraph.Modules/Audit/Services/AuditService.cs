using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Audit.Domain;
using PesaGraph.Audit.Repositories;
using PesaGraph.Shared.Errors;
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
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Audit.TenantRequired", "A tenant identifier is required."));
        }

        if (string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(action) ||
            string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
        {
            return Result.Failure(Error.Validation("Audit.RequiredFields", "Actor, action, resource type, and resource identifier are required."));
        }

        var log = AuditLog.Create(tenantId, actor, action, resourceType, resourceId, details);
        await _auditRepository.AddLogAsync(log, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AuditLog>>> GetAuditLogsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<AuditLog>>(Error.Validation("Audit.TenantRequired", "A tenant identifier is required."));
        }

        if (limit is < 1 or > 1_000)
        {
            return Result.Failure<IReadOnlyList<AuditLog>>(Error.Validation("Audit.InvalidLimit", "The audit log limit must be between 1 and 1,000."));
        }

        var list = await _auditRepository.ListLogsByTenantAsync(tenantId, limit, cancellationToken);
        return Result.Success(list);
    }
}
