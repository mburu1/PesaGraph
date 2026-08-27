using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Reconciliation.Repositories;
using PesaGraph.Shared.Enums;
using PesaGraph.Shared.Errors;
using PesaGraph.Shared.Results;

namespace PesaGraph.Reconciliation.Services;

public record RegisterUnmatchedItemRequest(
    Guid TenantId,
    Guid TransactionId,
    string ExternalReference,
    PaymentRail Rail,
    decimal Amount,
    string Counterparty);

public record ResolveItemRequest(
    Guid TenantId,
    string ExternalReference,
    string ResolvedBy,
    string Notes);

public record MatchCandidate(
    Guid TransactionId,
    string Reference,
    PaymentRail Rail,
    decimal Amount,
    DateTimeOffset Timestamp);

public interface IReconciliationService
{
    Task<Result<UnmatchedItem>> RegisterUnmatchedAsync(RegisterUnmatchedItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<MatchedPair>> AttemptMatchAsync(Guid tenantId, MatchCandidate source, MatchCandidate target, CancellationToken cancellationToken = default);
    Task<Result<UnmatchedItem>> ResolveItemAsync(ResolveItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UnmatchedItem>>> GetUnmatchedQueueAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MatchedPair>>> GetMatchedPairsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default);
}

public class ReconciliationService : IReconciliationService
{
    private readonly IReconciliationRepository _reconciliationRepository;

    public ReconciliationService(IReconciliationRepository reconciliationRepository)
    {
        _reconciliationRepository = reconciliationRepository;
    }

    public async Task<Result<UnmatchedItem>> RegisterUnmatchedAsync(RegisterUnmatchedItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = UnmatchedItem.Create(
            request.TenantId,
            request.TransactionId,
            request.ExternalReference,
            request.Rail,
            request.Amount,
            request.Counterparty);

        await _reconciliationRepository.AddUnmatchedItemAsync(item, cancellationToken);
        return Result.Success(item);
    }

    public async Task<Result<MatchedPair>> AttemptMatchAsync(Guid tenantId, MatchCandidate source, MatchCandidate target, CancellationToken cancellationToken = default)
    {
        // 1. Exact reference and amount match
        if (source.Reference.Equals(target.Reference, StringComparison.OrdinalIgnoreCase) && source.Amount == target.Amount)
        {
            var matchedPair = new MatchedPair(
                Guid.NewGuid(),
                tenantId,
                source.TransactionId,
                source.Reference,
                source.Rail,
                target.TransactionId,
                target.Reference,
                target.Rail,
                source.Amount,
                MatchConfidence.Exact,
                "Exact_Reference_And_Amount_Rule");

            await _reconciliationRepository.AddMatchedPairAsync(matchedPair, cancellationToken);
            return Result.Success(matchedPair);
        }

        // 2. Amount and time-window match (+- 3 minutes window)
        var timeDifference = (source.Timestamp - target.Timestamp).Duration();
        if (source.Amount == target.Amount && timeDifference <= TimeSpan.FromMinutes(3))
        {
            var matchedPair = new MatchedPair(
                Guid.NewGuid(),
                tenantId,
                source.TransactionId,
                source.Reference,
                source.Rail,
                target.TransactionId,
                target.Reference,
                target.Rail,
                source.Amount,
                MatchConfidence.High,
                "Amount_And_TimeWindow_Proximity_Rule");

            await _reconciliationRepository.AddMatchedPairAsync(matchedPair, cancellationToken);
            return Result.Success(matchedPair);
        }

        return Result.Failure<MatchedPair>(Error.NotFound("Reconciliation.NoMatch", "No automatic matching rule criteria satisfied."));
    }

    public async Task<Result<UnmatchedItem>> ResolveItemAsync(ResolveItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _reconciliationRepository.GetUnmatchedByReferenceAsync(request.TenantId, request.ExternalReference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<UnmatchedItem>(Error.NotFound("Reconciliation.ItemNotFound", $"Unmatched item with reference '{request.ExternalReference}' was not found."));
        }

        item.ResolveManually(request.ResolvedBy, request.Notes);
        await _reconciliationRepository.UpdateUnmatchedItemAsync(item, cancellationToken);

        return Result.Success(item);
    }

    public async Task<Result<IReadOnlyList<UnmatchedItem>>> GetUnmatchedQueueAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _reconciliationRepository.ListUnmatchedItemsAsync(tenantId, MatchStatus.Unmatched, cancellationToken);
        return Result.Success(list);
    }

    public async Task<Result<IReadOnlyList<MatchedPair>>> GetMatchedPairsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _reconciliationRepository.ListMatchedPairsAsync(tenantId, limit, cancellationToken);
        return Result.Success(list);
    }
}
