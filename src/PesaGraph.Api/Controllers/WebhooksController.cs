using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PesaGraph.Ingestion.Services;
using PesaGraph.Providers.Daraja.Models;
using PesaGraph.Shared.Enums;

namespace PesaGraph.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IIngestionService _ingestionService;

    public WebhooksController(IIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    [HttpPost("mpesa/c2b-validation")]
    public IActionResult MpesaValidation([FromBody] DarajaC2BValidationRequest request)
    {
        // Auto-accept C2B validation for registered tenants
        return Ok(new DarajaC2BConfirmationResponse("0", "Accepted"));
    }

    [HttpPost("mpesa/c2b-confirmation")]
    public async Task<IActionResult> MpesaConfirmation(
        [FromHeader(Name = "X-Tenant-Id")] Guid? headerTenantId,
        [FromBody] JsonElement rawPayload,
        CancellationToken cancellationToken)
    {
        var tenantId = headerTenantId ?? Guid.Empty;
        var transId = rawPayload.TryGetProperty("TransID", out var tId) ? tId.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

        var result = await _ingestionService.IngestWebhookAsync(new IngestWebhookRequest(
            tenantId,
            PaymentRail.Mpesa,
            "C2B_CONFIRMATION",
            transId,
            rawPayload.ToString(),
            null), cancellationToken);

        return result.IsSuccess
            ? Ok(new DarajaC2BConfirmationResponse("0", "Success"))
            : BadRequest(result.Error);
    }

    [HttpPost("airtel")]
    public async Task<IActionResult> AirtelCallback(
        [FromHeader(Name = "X-Tenant-Id")] Guid? headerTenantId,
        [FromBody] JsonElement rawPayload,
        CancellationToken cancellationToken)
    {
        var tenantId = headerTenantId ?? Guid.Empty;
        var transId = Guid.NewGuid().ToString();

        var result = await _ingestionService.IngestWebhookAsync(new IngestWebhookRequest(
            tenantId,
            PaymentRail.AirtelMoney,
            "AIRTEL_CALLBACK",
            transId,
            rawPayload.ToString(),
            null), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
