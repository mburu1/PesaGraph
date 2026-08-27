using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Liquidity.Services;
using PesaGraph.Providers.WhatsApp;
using PesaGraph.Reconciliation.Services;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Conversational.Services;

public record InboundCommand(
    Guid TenantId,
    string SenderPhone,
    string RawText,
    string Channel = "WhatsApp");

public interface IConversationalCommandService
{
    Task<Result<string>> HandleCommandAsync(InboundCommand command, CancellationToken cancellationToken = default);
}

public class ConversationalCommandService : IConversationalCommandService
{
    private readonly ILiquidityService _liquidityService;
    private readonly IReconciliationService _reconciliationService;
    private readonly IWhatsAppClient _whatsAppClient;

    public ConversationalCommandService(
        ILiquidityService liquidityService,
        IReconciliationService reconciliationService,
        IWhatsAppClient whatsAppClient)
    {
        _liquidityService = liquidityService;
        _reconciliationService = reconciliationService;
        _whatsAppClient = whatsAppClient;
    }

    public async Task<Result<string>> HandleCommandAsync(InboundCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TenantId == Guid.Empty)
        {
            return Result.Failure<string>(Error.Validation("Conversational.TenantRequired", "A tenant identifier is required."));
        }

        var text = command.RawText.Trim();
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mainCommand = parts.Length > 0 ? parts[0].ToLowerInvariant() : "help";

        string responseMessage;

        switch (mainCommand)
        {
            case "float":
            case "balance":
                responseMessage = await HandleFloatCommandAsync(command.TenantId, cancellationToken);
                break;

            case "unmatched":
            case "pending":
                responseMessage = await HandleUnmatchedCommandAsync(command.TenantId, cancellationToken);
                break;

            case "resolve":
                var refToResolve = parts.Length > 1 ? parts[1] : string.Empty;
                responseMessage = await HandleResolveCommandAsync(command.TenantId, refToResolve, command.SenderPhone, cancellationToken);
                break;

            case "help":
            default:
                responseMessage =
                    "🟢 *PesaGraph Operations Assistant*\n\n" +
                    "Available Commands:\n" +
                    "• *float* — Current balances across M-Pesa, Airtel & Banks\n" +
                    "• *unmatched* — List pending unreconciled transactions\n" +
                    "• *resolve <REF>* — Manually mark an item as reconciled\n" +
                    "• *help* — Show this instructions menu";
                break;
        }

        // Send via WhatsApp if channel is WhatsApp and SenderPhone is present
        if (command.Channel.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(command.SenderPhone))
        {
            var sendResult = await _whatsAppClient.SendTextMessageAsync(command.SenderPhone, responseMessage, cancellationToken);
            if (sendResult.IsFailure)
            {
                return Result.Failure<string>(sendResult.Error);
            }
        }

        return Result.Success(responseMessage);
    }

    private async Task<string> HandleFloatCommandAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var floatResult = await _liquidityService.GetFloatCockpitAsync(tenantId, 50000m, cancellationToken);
        if (floatResult.IsFailure) return $"⚠️ Error fetching float: {floatResult.Error.Description}";

        var cockpit = floatResult.Value;
        var sb = new StringBuilder();
        sb.AppendLine("📊 *PesaGraph Real-Time Float Cockpit*");
        sb.AppendLine($"💰 *Total Liquidity:* KES {cockpit.TotalLiquidFloat:N2}\n");
        sb.AppendLine($"• *M-Pesa:* KES {cockpit.MpesaFloat:N2}");
        sb.AppendLine($"• *Airtel Money:* KES {cockpit.AirtelFloat:N2}");
        sb.AppendLine($"• *Banks:* KES {cockpit.BankFloat:N2}");
        sb.AppendLine($"• *Cash Float:* KES {cockpit.CashFloat:N2}");

        if (cockpit.ActiveAlerts.Count > 0)
        {
            sb.AppendLine("\n⚠️ *Alerts:*");
            foreach (var alert in cockpit.ActiveAlerts)
            {
                sb.AppendLine($"• {alert.Message}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> HandleUnmatchedCommandAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var unmatchedResult = await _reconciliationService.GetUnmatchedQueueAsync(tenantId, cancellationToken);
        if (unmatchedResult.IsFailure) return $"⚠️ Error fetching queue: {unmatchedResult.Error.Description}";

        var items = unmatchedResult.Value;
        if (items.Count == 0) return "✅ All clear! There are currently no unmatched items in the reconciliation queue.";

        var sb = new StringBuilder();
        sb.AppendLine($"🔍 *Unmatched Items ({items.Count}):*\n");

        foreach (var item in items)
        {
            sb.AppendLine($"• *{item.ExternalReference}* | {item.Rail} | KES {item.Amount:N2} | {item.Counterparty}");
        }

        sb.AppendLine("\n_To resolve an item, reply:_ *resolve <REF>*");
        return sb.ToString();
    }

    private async Task<string> HandleResolveCommandAsync(Guid tenantId, string reference, string senderPhone, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return "⚠️ Please provide a valid transaction reference. Example: *resolve QKH12345*";
        }

        var resolveResult = await _reconciliationService.ResolveItemAsync(new ResolveItemRequest(
            tenantId,
            reference,
            $"WhatsApp:{senderPhone}",
            "Resolved via WhatsApp command"), cancellationToken);

        return resolveResult.IsSuccess
            ? $"✅ Item *{reference}* has been successfully marked as reconciled in the ledger."
            : $"❌ Could not resolve item *{reference}*: {resolveResult.Error.Description}";
    }
}
