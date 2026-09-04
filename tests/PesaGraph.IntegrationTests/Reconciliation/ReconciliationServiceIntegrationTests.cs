using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Reconciliation.Repositories;
using PesaGraph.Reconciliation.Services;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.IntegrationTests.Reconciliation;

public class ReconciliationServiceIntegrationTests : IAsyncLifetime
{
    private readonly InMemoryReconciliationRepository _repository;
    private readonly ReconciliationService _reconciliationService;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ReconciliationServiceIntegrationTests()
    {
        _repository = new InMemoryReconciliationRepository();
        _reconciliationService = new ReconciliationService(_repository);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RegisterUnmatchedItem_AndRetrieve_ShouldPersist()
    {
        var request = new RegisterUnmatchedItemRequest(
            TenantId,
            Guid.NewGuid(),
            "EXT-REF-001",
            PaymentRail.Mpesa,
            1000m,
            "Test Counterparty");

        var recordResult = await _reconciliationService.RegisterUnmatchedAsync(request);

        recordResult.IsSuccess.Should().BeTrue();

        var retrieveResult = await _reconciliationService.GetUnmatchedQueueAsync(TenantId);
        retrieveResult.IsSuccess.Should().BeTrue();
        retrieveResult.Value.Should().Contain(item => item.ExternalReference == "EXT-REF-001");
        retrieveResult.Value.Should().Contain(item => item.Amount == 1000m);
    }

    [Fact]
    public async Task AttemptMatch_ExactReferenceAndAmount_ShouldMatch()
    {
        var source = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            DateTimeOffset.UtcNow);

        var target = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            DateTimeOffset.UtcNow);

        var matchResult = await _reconciliationService.AttemptMatchAsync(TenantId, source, target);

        matchResult.IsSuccess.Should().BeTrue();
        matchResult.Value.Confidence.Should().Be(MatchConfidence.Exact);
    }

    [Fact]
    public async Task AttemptMatch_AmountAndTimeWindow_ShouldMatchWithHighConfidence()
    {
        var now = DateTimeOffset.UtcNow;
        var source = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            now);

        var target = new MatchCandidate(
            Guid.NewGuid(),
            "INT-001",
            PaymentRail.Mpesa,
            500m,
            now.AddMinutes(2));

        var matchResult = await _reconciliationService.AttemptMatchAsync(TenantId, source, target);

        matchResult.IsSuccess.Should().BeTrue();
        matchResult.Value.Confidence.Should().Be(MatchConfidence.High);
    }

    [Fact]
    public async Task AttemptMatch_NoMatch_ShouldReturnFailure()
    {
        var source = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            DateTimeOffset.UtcNow);

        var target = new MatchCandidate(
            Guid.NewGuid(),
            "INT-001",
            PaymentRail.Mpesa,
            999m,
            DateTimeOffset.UtcNow);

        var matchResult = await _reconciliationService.AttemptMatchAsync(TenantId, source, target);

        matchResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetUnmatchedQueue_ShouldReturnFiltered()
    {
        var request1 = new RegisterUnmatchedItemRequest(
            TenantId, Guid.NewGuid(), "EXT-001", PaymentRail.Mpesa, 1000m, "Counterparty 1");
        var request2 = new RegisterUnmatchedItemRequest(
            TenantId, Guid.NewGuid(), "EXT-002", PaymentRail.AirtelMoney, 2000m, "Counterparty 2");

        await _reconciliationService.RegisterUnmatchedAsync(request1);
        await _reconciliationService.RegisterUnmatchedAsync(request2);

        var unmatchedResult = await _reconciliationService.GetUnmatchedQueueAsync(TenantId);

        unmatchedResult.IsSuccess.Should().BeTrue();
        unmatchedResult.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMatchedPairs_ShouldReturnMatchedItems()
    {
        var source = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            DateTimeOffset.UtcNow);

        var target = new MatchCandidate(
            Guid.NewGuid(),
            "EXT-001",
            PaymentRail.Mpesa,
            500m,
            DateTimeOffset.UtcNow);

        await _reconciliationService.AttemptMatchAsync(TenantId, source, target);

        var matchedResult = await _reconciliationService.GetMatchedPairsAsync(TenantId);

        matchedResult.IsSuccess.Should().BeTrue();
        matchedResult.Value.Should().HaveCount(1);
    }

    private class InMemoryReconciliationRepository : IReconciliationRepository
    {
        private readonly Dictionary<Guid, UnmatchedItem> _items = new();
        private readonly List<MatchedPair> _matchedPairs = new();

        public Task AddMatchedPairAsync(MatchedPair pair, CancellationToken cancellationToken = default)
        {
            _matchedPairs.Add(pair);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MatchedPair>> ListMatchedPairsAsync(Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
        {
            var list = _matchedPairs
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.MatchedAtUtc)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<MatchedPair>>(list);
        }

        public Task AddUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default)
        {
            _items[item.Id] = item;
            return Task.CompletedTask;
        }

        public Task<UnmatchedItem?> GetUnmatchedByReferenceAsync(Guid tenantId, string reference, CancellationToken cancellationToken = default)
        {
            var item = _items.Values.FirstOrDefault(u => u.TenantId == tenantId && u.ExternalReference.Equals(reference, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<UnmatchedItem>> ListUnmatchedItemsAsync(Guid tenantId, MatchStatus? status = MatchStatus.Unmatched, CancellationToken cancellationToken = default)
        {
            var query = _items.Values.Where(u => u.TenantId == tenantId);
            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            var list = query.OrderByDescending(u => u.CreatedAtUtc).ToList();
            return Task.FromResult<IReadOnlyList<UnmatchedItem>>(list);
        }

        public Task UpdateUnmatchedItemAsync(UnmatchedItem item, CancellationToken cancellationToken = default)
        {
            _items[item.Id] = item;
            return Task.CompletedTask;
        }
    }
}
