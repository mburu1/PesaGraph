using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PesaGraph.Conversational.Services;
using PesaGraph.Providers.Options;
using PesaGraph.Providers.WhatsApp.Models;

namespace PesaGraph.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ConversationalWebhookController : ControllerBase
{
    private readonly IConversationalCommandService _commandService;
    private readonly WhatsAppOptions _whatsAppOptions;

    public ConversationalWebhookController(
        IConversationalCommandService commandService,
        IOptions<WhatsAppOptions> whatsAppOptions)
    {
        _commandService = commandService;
        _whatsAppOptions = whatsAppOptions.Value;
    }

    /// <summary>
    /// Meta Cloud API Webhook Verification Endpoint (GET)
    /// </summary>
    [HttpGet("whatsapp")]
    public IActionResult VerifyWhatsAppWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken)
    {
        if (mode == "subscribe" && verifyToken == _whatsAppOptions.WebhookVerifyToken)
        {
            return Ok(challenge);
        }

        return Forbid();
    }

    /// <summary>
    /// Inbound WhatsApp message receiver (POST)
    /// </summary>
    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceiveWhatsAppMessage(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromBody] WhatsAppWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        var targetTenantId = tenantId ?? Guid.Empty;

        if (payload.Entry != null)
        {
            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;
                foreach (var change in entry.Changes)
                {
                    if (change.Value?.Messages == null) continue;
                    foreach (var msg in change.Value.Messages)
                    {
                        if (msg.Text != null && !string.IsNullOrWhiteSpace(msg.Text.Body))
                        {
                            await _commandService.HandleCommandAsync(new InboundCommand(
                                targetTenantId,
                                msg.From,
                                msg.Text.Body,
                                Channel: "WhatsApp"), cancellationToken);
                        }
                    }
                }
            }
        }

        return Ok(new { status = "received" });
    }

    /// <summary>
    /// Inbound SMS message receiver (POST)
    /// </summary>
    [HttpPost("sms")]
    public async Task<IActionResult> ReceiveSms(
        [FromHeader(Name = "X-Tenant-Id")] Guid? tenantId,
        [FromForm] string from,
        [FromForm] string text,
        CancellationToken cancellationToken)
    {
        var targetTenantId = tenantId ?? Guid.Empty;
        var result = await _commandService.HandleCommandAsync(new InboundCommand(
            targetTenantId,
            from,
            text,
            Channel: "SMS"), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
