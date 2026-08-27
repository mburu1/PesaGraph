using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PesaGraph.Liquidity.Services;

namespace PesaGraph.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LiquidityController : ControllerBase
{
    private readonly ILiquidityService _liquidityService;

    public LiquidityController(ILiquidityService liquidityService)
    {
        _liquidityService = liquidityService;
    }

    [HttpGet("cockpit")]
    public async Task<IActionResult> GetCockpit(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromQuery] decimal lowFloatThreshold = 50000m,
        CancellationToken cancellationToken = default)
    {
        var targetTenantId = tenantId ?? Guid.Empty;
        var result = await _liquidityService.GetFloatCockpitAsync(targetTenantId, lowFloatThreshold, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
