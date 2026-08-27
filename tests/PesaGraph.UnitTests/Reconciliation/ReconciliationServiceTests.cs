using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Reconciliation.Repositories;
using PesaGraph.Reconciliation.Services;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Reconciliation;

public class ReconciliationServiceTests
{
    private readonly IReconciliationRepository _repository;
    private readonly ReconciliationService _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ReconciliationServiceTests()
    {
        _repository = Substitute.For<IReconciliationRepository>();
        _sut = new ReconciliationService(_repository);
    }

    [Fact]
    public async Task RegisterUnmatchedAsync_ShouldCreateAndPersistUnmatchedItem()
    {
        var request = new RegisterUnmatchedItemRequest(
            TenantId, Guid.NewGuid(), "EXT-REF-01", PaymentRail.Mpesa, 2500m, "Jane");

        var result = await _sut.RegisterUnmatchedAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalReference.Should().Be("EXT-REF-01");
        result.Value.Status.Should().Be(MatchStatus.Unmatched);
        await _repository.Received(1).AddUnmatchedItemAsync(Arg.Any<UnmatchedItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AttemptMatchAsync_WithExactReferenceAndAmount_ShouldReturnExactMatch()
    {
        var source = new MatchCandidate(Guid.NewGuid(), "REF-XYZ", PaymentRail.Mpesa, 1000m, DateTimeOffset.UtcNow);
        var target = new MatchCandidate(Guid.NewGuid(), "REF-XYZ", PaymentRail.Bank, 1000m, DateTimeOffset.UtcNow);

        var result = await _sut.AttemptMatchAsync(TenantId, source, target);

        result.IsSuccess.Should().BeTrue();
        result.Value.Confidence.Should().Be(MatchConfidence.Exact);
        result.Value.RuleName.Should().Be("Exact_Reference_And_Amount_Rule");
        await _repository.Received(1).AddMatchedPairAsync(Arg.Any<MatchedPair>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AttemptMatchAsync_WithSameAmountWithinTimeWindow_ShouldReturnHighConfidenceMatch()
    {
        var now = DateTimeOffset.UtcNow;
        var source = new MatchCandidate(Guid.NewGuid(), "REF-A", PaymentRail.Mpesa, 5000m, now);
        var target = new MatchCandidate(Guid.NewGuid(), "REF-B", PaymentRail.Bank, 5000m, now.AddMinutes(2));

        var result = await _sut.AttemptMatchAsync(TenantId, source, target);

        result.IsSuccess.Should().BeTrue();
        result.Value.Confidence.Should().Be(MatchConfidence.High);
        result.Value.RuleName.Should().Be("Amount_And_TimeWindow_Proximity_Rule");
    }

    [Fact]
    public async Task AttemptMatchAsync_WithNoMatchingCriteria_ShouldReturnFailure()
    {
        var source = new MatchCandidate(Guid.NewGuid(), "REF-1", PaymentRail.Mpesa, 1000m, DateTimeOffset.UtcNow);
        var target = new MatchCandidate(Guid.NewGuid(), "REF-2", PaymentRail.Bank, 9999m, DateTimeOffset.UtcNow.AddHours(1));

        var result = await _sut.AttemptMatchAsync(TenantId, source, target);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Reconciliation.NoMatch");
    }

    [Fact]
    public async Task AttemptMatchAsync_SameAmountOutsideTimeWindow_ShouldReturnFailure()
    {
        var source = new MatchCandidate(Guid.NewGuid(), "REF-1", PaymentRail.Mpesa, 500m, DateTimeOffset.UtcNow);
        var target = new MatchCandidate(Guid.NewGuid(), "REF-2", PaymentRail.Bank, 500m, DateTimeOffset.UtcNow.AddMinutes(5));

        var result = await _sut.AttemptMatchAsync(TenantId, source, target);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveItemAsync_WhenItemExists_ShouldResolveSuccessfully()
    {
        var item = UnmatchedItem.Create(TenantId, Guid.NewGuid(), "EXT-REF-RESOLVE", PaymentRail.Mpesa, 1500m, "Bob");
        var request = new ResolveItemRequest(TenantId, "EXT-REF-RESOLVE", "admin@acme.com", "Confirmed by ops team.");

        _repository.GetUnmatchedByReferenceAsync(TenantId, "EXT-REF-RESOLVE", Arg.Any<CancellationToken>())
            .Returns(item);

        var result = await _sut.ResolveItemAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MatchStatus.ManuallyResolved);
        await _repository.Received(1).UpdateUnmatchedItemAsync(Arg.Any<UnmatchedItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveItemAsync_WhenItemNotFound_ShouldReturnNotFoundError()
    {
        _repository.GetUnmatchedByReferenceAsync(TenantId, "MISSING-REF", Arg.Any<CancellationToken>())
            .Returns((UnmatchedItem?)null);

        var request = new ResolveItemRequest(TenantId, "MISSING-REF", "admin", "notes");

        var result = await _sut.ResolveItemAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Reconciliation.ItemNotFound");
    }
}
