using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PesaGraph.Tenancy.Domain;
using PesaGraph.Tenancy.DTOs;
using PesaGraph.Tenancy.Services;

namespace PesaGraph.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.CreateTenantAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetTenantById), new { id = result.Value.Id }, result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenantById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> ListTenants([FromQuery] TenantStatus? status, CancellationToken cancellationToken)
    {
        var result = await _tenantService.ListTenantsAsync(status, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/api-keys")]
    public async Task<IActionResult> GenerateApiKey(Guid id, [FromBody] GenerateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GenerateApiKeyAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
