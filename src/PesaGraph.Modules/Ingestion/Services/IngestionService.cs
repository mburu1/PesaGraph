using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Ingestion.Domain;
using PesaGraph.Ingestion.Events;
using PesaGraph.Ingestion.Normalisers;
using PesaGraph.Ingestion.Repositories;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Ingestion.Services;

public record IngestWebhookRequest(
    Guid TenantId,
    PaymentRail Rail,
    string EventType,
    string ExternalReference,
    string RawJson,
    string? HeaderJson);

public interface IIngestionService
{
    Task<Result<CanonicalTransactionIngestedEvent>> IngestWebhookAsync(IngestWebhookRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RawWebhookEvent>>> GetIngestionHistoryAsync(Guid tenantId, int limit = 50, CancellationToken cancellationToken = default);
}

public class IngestionService : IIngestionService
{
    private readonly IRawWebhookRepository _rawRepository;
    private readonly IEnumerable<IPayloadNormaliser> _normalisers;

    public IngestionService(
        IRawWebhookRepository rawRepository,
        IEnumerable<IPayloadNormaliser> normalisers)
    {
        _rawRepository = rawRepository;
        _normalisers = normalisers;
    }

    public async Task<Result<CanonicalTransactionIngestedEvent>> IngestWebhookAsync(IngestWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = $"{request.Rail}_{request.ExternalReference}";

        // 1. Idempotency check
        var existing = await _rawRepository.GetByIdempotencyKeyAsync(request.TenantId, idempotencyKey, cancellationToken);
        if (existing != null)
        {
            existing.MarkDuplicate();
            await _rawRepository.UpdateAsync(existing, cancellationToken);
            return Result.Failure<CanonicalTransactionIngestedEvent>(Error.Conflict("Ingestion.Duplicate", $"Webhook event '{idempotencyKey}' has already been processed."));
        }

        // 2. Persist Raw Webhook Event
        var rawEvent = RawWebhookEvent.Create(
            request.TenantId,
            request.Rail,
            request.EventType,
            request.ExternalReference,
            request.RawJson,
            request.HeaderJson,
            idempotencyKey);

        await _rawRepository.AddAsync(rawEvent, cancellationToken);

        // 3. Find normaliser
        var normaliser = _normalisers.FirstOrDefault(n => n.Rail == request.Rail && n.CanHandle(request.EventType));
        if (normaliser == null)
        {
            rawEvent.MarkFailed("No matching normaliser found for event type.");
            await _rawRepository.UpdateAsync(rawEvent, cancellationToken);
            return Result.Failure<CanonicalTransactionIngestedEvent>(Error.Failure("Ingestion.NoNormaliser", $"No normaliser registered for rail {request.Rail} and event type {request.EventType}."));
        }

        // 4. Normalise to Canonical Transaction
        var normaliseResult = normaliser.Normalise(rawEvent);
        if (normaliseResult.IsFailure)
        {
            rawEvent.MarkFailed(normaliseResult.Error.Description);
            await _rawRepository.UpdateAsync(rawEvent, cancellationToken);
            return Result.Failure<CanonicalTransactionIngestedEvent>(normaliseResult.Error);
        }

        rawEvent.MarkNormalised();
        await _rawRepository.UpdateAsync(rawEvent, cancellationToken);

        return Result.Success(normaliseResult.Value);
    }

    public async Task<Result<IReadOnlyList<RawWebhookEvent>>> GetIngestionHistoryAsync(Guid tenantId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var list = await _rawRepository.ListByTenantAsync(tenantId, limit, cancellationToken);
        return Result.Success(list);
    }
}
