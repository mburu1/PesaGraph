using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PesaGraph.Reconciliation.Services;

namespace PesaGraph.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;

    public ReconciliationController(IReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    [HttpGet("unmatched")]
    public async Task<IActionResult> GetUnmatchedQueue(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var targetTenantId = tenantId ?? Guid.Empty;
        var result = await _reconciliationService.GetUnmatchedQueueAsync(targetTenantId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("matched")]
    public async Task<IActionResult> GetMatchedPairs(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var targetTenantId = tenantId ?? Guid.Empty;
        var result = await _reconciliationService.GetMatchedPairsAsync(targetTenantId, limit, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("resolve")]
    public async Task<IActionResult> ResolveItem(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromBody] ResolveItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetTenantId = tenantId ?? request.TenantId;
        var updatedRequest = request with { TenantId = targetTenantId };
        var result = await _reconciliationService.ResolveItemAsync(updatedRequest, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
