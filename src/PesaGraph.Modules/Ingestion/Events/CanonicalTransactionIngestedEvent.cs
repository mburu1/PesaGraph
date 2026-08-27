using System;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Messaging;

namespace PesaGraph.Ingestion.Events;

public record CanonicalTransactionIngestedEvent(
    Guid TransactionId,
    Guid TenantId,
    PaymentRail Rail,
    TransactionType Type,
    string ExternalReference,
    decimal Amount,
    string Currency,
    decimal FeeAmount,
    string AccountNumber,
    string CounterpartyName,
    string CounterpartyPhone,
    DateTimeOffset TimestampUtc,
    string RawPayloadId) : IntegrationEvent
{
    public new Guid TenantId { get; init; } = TenantId;
}
