using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ingestion.Domain;
using PesaGraph.Ingestion.Events;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Ingestion.Normalisers;

public interface IPayloadNormaliser
{
    PaymentRail Rail { get; }
    bool CanHandle(string eventType);
    Result<CanonicalTransactionIngestedEvent> Normalise(RawWebhookEvent rawEvent);
}

public class DarajaC2BNormaliser : IPayloadNormaliser
{
    public PaymentRail Rail => PaymentRail.Mpesa;

    public bool CanHandle(string eventType) =>
        eventType.Equals("C2B_CONFIRMATION", StringComparison.OrdinalIgnoreCase) ||
        eventType.Equals("STK_CALLBACK", StringComparison.OrdinalIgnoreCase);

    public Result<CanonicalTransactionIngestedEvent> Normalise(RawWebhookEvent rawEvent)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawEvent.RawJson);
            var root = doc.RootElement;

            if (rawEvent.EventType.Equals("C2B_CONFIRMATION", StringComparison.OrdinalIgnoreCase))
            {
                var transId = root.GetProperty("TransID").GetString() ?? rawEvent.ExternalReference;
                var transAmountStr = root.GetProperty("TransAmount").GetString() ?? "0";
                decimal.TryParse(transAmountStr, out var amount);
                var phone = root.GetProperty("MSISDN").GetString() ?? string.Empty;
                var shortCode = root.GetProperty("BusinessShortCode").GetString() ?? string.Empty;
                var billRef = root.TryGetProperty("BillRefNumber", out var bRef) ? bRef.GetString() : null;
                var firstName = root.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "";
                var lastName = root.TryGetProperty("LastName", out var ln) ? ln.GetString() : "";
                var name = $"{firstName} {lastName}".Trim();

                var canonical = new CanonicalTransactionIngestedEvent(
                    Guid.NewGuid(),
                    rawEvent.TenantId,
                    PaymentRail.Mpesa,
                    TransactionType.CustomerToBusiness,
                    transId,
                    amount,
                    "KES",
                    0m,
                    shortCode,
                    name,
                    phone,
                    DateTimeOffset.UtcNow,
                    rawEvent.Id.ToString());

                return Result.Success(canonical);
            }

            return Result.Failure<CanonicalTransactionIngestedEvent>(Error.Failure("Normaliser.Unsupported", "Unhandled Daraja event sub-type."));
        }
        catch (Exception ex)
        {
            return Result.Failure<CanonicalTransactionIngestedEvent>(Error.Failure("Normaliser.Exception", ex.Message));
        }
    }
}

public class AirtelMoneyNormaliser : IPayloadNormaliser
{
    public PaymentRail Rail => PaymentRail.AirtelMoney;

    public bool CanHandle(string eventType) =>
        eventType.Equals("AIRTEL_CALLBACK", StringComparison.OrdinalIgnoreCase);

    public Result<CanonicalTransactionIngestedEvent> Normalise(RawWebhookEvent rawEvent)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawEvent.RawJson);
            var root = doc.RootElement;
            var trans = root.GetProperty("transaction");
            var transId = trans.GetProperty("id").GetString() ?? rawEvent.ExternalReference;
            var airtelMoneyId = trans.TryGetProperty("airtel_money_id", out var mid) ? mid.GetString() : transId;

            var canonical = new CanonicalTransactionIngestedEvent(
                Guid.NewGuid(),
                rawEvent.TenantId,
                PaymentRail.AirtelMoney,
                TransactionType.CustomerToBusiness,
                airtelMoneyId ?? transId,
                0m,
                "KES",
                0m,
                "AirtelWallet",
                "Airtel Customer",
                "",
                DateTimeOffset.UtcNow,
                rawEvent.Id.ToString());

            return Result.Success(canonical);
        }
        catch (Exception ex)
        {
            return Result.Failure<CanonicalTransactionIngestedEvent>(Error.Failure("Normaliser.Exception", ex.Message));
        }
    }
}
