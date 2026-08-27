using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using PesaGraph.Ingestion.Events;
using PesaGraph.Ledger.Services;
using PesaGraph.Reconciliation.Services;

namespace PesaGraph.Workers.Consumers;

public class CanonicalTransactionConsumer : IConsumer<CanonicalTransactionIngestedEvent>
{
    private readonly ILedgerService _ledgerService;
    private readonly IReconciliationService _reconciliationService;
    private readonly ILogger<CanonicalTransactionConsumer> _logger;

    public CanonicalTransactionConsumer(
        ILedgerService ledgerService,
        IReconciliationService reconciliationService,
        ILogger<CanonicalTransactionConsumer> logger)
    {
        _ledgerService = ledgerService;
        _reconciliationService = reconciliationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CanonicalTransactionIngestedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing canonical transaction {TransactionId} for Tenant {TenantId}, Rail: {Rail}, Ref: {Ref}",
            message.TransactionId, message.TenantId, message.Rail, message.ExternalReference);

        // 1. Post to Canonical Ledger
        var postResult = await _ledgerService.PostTransactionAsync(new PostTransactionRequest(
            message.TenantId,
            message.AccountNumber,
            message.TransactionId,
            message.ExternalReference,
            message.Amount,
            message.FeeAmount,
            message.Rail,
            message.Type,
            $"{message.CounterpartyName} ({message.CounterpartyPhone})",
            $"Ingested from {message.Rail}",
            message.Currency), context.CancellationToken);

        if (postResult.IsFailure)
        {
            _logger.LogError("Failed to post transaction {TransactionId} to ledger: {Error}", message.TransactionId, postResult.Error.Description);
        }

        // 2. Register in Reconciliation Unmatched Queue
        var queueResult = await _reconciliationService.RegisterUnmatchedAsync(new RegisterUnmatchedItemRequest(
            message.TenantId,
            message.TransactionId,
            message.ExternalReference,
            message.Rail,
            message.Amount,
            $"{message.CounterpartyName} ({message.CounterpartyPhone})"), context.CancellationToken);

        if (queueResult.IsSuccess)
        {
            _logger.LogInformation("Transaction {TransactionId} registered in unmatched queue with ref {Ref}", message.TransactionId, message.ExternalReference);
        }
    }
}
