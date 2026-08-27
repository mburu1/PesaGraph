using System;
using System.Threading.Tasks;
using FluentAssertions;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Reconciliation.Repositories;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Reconciliation;

/// <summary>
/// Tests for the InMemoryReconciliationRepository — a fast in-memory implementation
/// suitable for use in tests and local development without external dependencies.
/// </summary>
public class InMemoryReconciliationRepositoryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly InMemoryReconciliationRepository _sut = new();

    private static UnmatchedItem CreateUnmatchedItem(string reference = "REF-TEST")
    {
        return UnmatchedItem.Create(TenantId, Guid.NewGuid(), reference, PaymentRail.Mpesa, 1000m, "John");
    }

    private static MatchedPair CreateMatchedPair()
    {
        return new MatchedPair(
            Guid.NewGuid(),
            TenantId,
            Guid.NewGuid(),
            "SRC-REF",
            PaymentRail.Mpesa,
            Guid.NewGuid(),
            "TGT-REF",
            PaymentRail.Bank,
            1000m,
            MatchConfidence.Exact,
            "TestRule");
    }

    [Fact]
    public async Task AddUnmatchedItemAsync_ShouldPersistItem()
    {
        var item = CreateUnmatchedItem();

        await _sut.AddUnmatchedItemAsync(item);

        var result = await _sut.GetUnmatchedByReferenceAsync(TenantId, item.ExternalReference);
        result.Should().NotBeNull();
        result!.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task GetUnmatchedByReferenceAsync_CaseInsensitive_ShouldFind()
    {
        var item = CreateUnmatchedItem("ref-case");
        await _sut.AddUnmatchedItemAsync(item);

        var result = await _sut.GetUnmatchedByReferenceAsync(TenantId, "REF-CASE");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnmatchedByReferenceAsync_WhenNotFound_ShouldReturnNull()
    {
        var result = await _sut.GetUnmatchedByReferenceAsync(TenantId, "NONEXISTENT");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListUnmatchedItemsAsync_ShouldReturnOnlyTenantItems()
    {
        var otherTenantId = Guid.NewGuid();
        var myItem = UnmatchedItem.Create(TenantId, Guid.NewGuid(), "MY-REF", PaymentRail.Mpesa, 100m, "Alice");
        var otherItem = UnmatchedItem.Create(otherTenantId, Guid.NewGuid(), "OTHER-REF", PaymentRail.Mpesa, 100m, "Bob");

        await _sut.AddUnmatchedItemAsync(myItem);
        await _sut.AddUnmatchedItemAsync(otherItem);

        var result = await _sut.ListUnmatchedItemsAsync(TenantId);

        result.Should().ContainSingle(i => i.Id == myItem.Id);
    }

    [Fact]
    public async Task UpdateUnmatchedItemAsync_ShouldReplaceItem()
    {
        var item = CreateUnmatchedItem("UPD-REF");
        await _sut.AddUnmatchedItemAsync(item);
        item.ResolveManually("admin", "Resolved");

        await _sut.UpdateUnmatchedItemAsync(item);

        var updated = await _sut.GetUnmatchedByReferenceAsync(TenantId, "UPD-REF");
        updated!.Status.Should().Be(MatchStatus.ManuallyResolved);
    }

    [Fact]
    public async Task AddMatchedPairAsync_ShouldPersistPair()
    {
        var pair = CreateMatchedPair();

        await _sut.AddMatchedPairAsync(pair);

        var list = await _sut.ListMatchedPairsAsync(TenantId);
        list.Should().ContainSingle(p => p.Id == pair.Id);
    }

    [Fact]
    public async Task ListMatchedPairsAsync_ShouldRespectLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            await _sut.AddMatchedPairAsync(CreateMatchedPair());
        }

        var result = await _sut.ListMatchedPairsAsync(TenantId, limit: 3);

        result.Should().HaveCount(3);
    }
}
