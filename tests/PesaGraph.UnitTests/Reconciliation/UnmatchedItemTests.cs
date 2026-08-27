using System;
using FluentAssertions;
using PesaGraph.Reconciliation.Domain;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Reconciliation;

public class UnmatchedItemTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static UnmatchedItem CreateItem(
        string externalReference = "TXN-001",
        PaymentRail rail = PaymentRail.Mpesa,
        decimal amount = 5000m,
        string counterparty = "John Doe")
    {
        return UnmatchedItem.Create(TenantId, Guid.NewGuid(), externalReference, rail, amount, counterparty);
    }

    [Fact]
    public void Create_ShouldReturnItemWithExpectedProperties()
    {
        var item = CreateItem();

        item.Id.Should().NotBe(Guid.Empty);
        item.TenantId.Should().Be(TenantId);
        item.ExternalReference.Should().Be("TXN-001");
        item.Rail.Should().Be(PaymentRail.Mpesa);
        item.Amount.Should().Be(5000m);
        item.Counterparty.Should().Be("John Doe");
        item.Status.Should().Be(MatchStatus.Unmatched);
        item.ResolutionNotes.Should().BeNull();
        item.ResolvedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAssignUniqueIds()
    {
        var item1 = CreateItem();
        var item2 = CreateItem();

        item1.Id.Should().NotBe(item2.Id);
    }

    [Fact]
    public void ResolveManually_ShouldUpdateStatusToManuallyResolved()
    {
        var item = CreateItem();

        item.ResolveManually("alice@acme.com", "Verified via bank statement.");

        item.Status.Should().Be(MatchStatus.ManuallyResolved);
    }

    [Fact]
    public void ResolveManually_ShouldSetResolutionNotes()
    {
        var item = CreateItem();

        item.ResolveManually("alice@acme.com", "Matched manually.");

        item.ResolutionNotes.Should().Contain("Matched manually.");
        item.ResolutionNotes.Should().Contain("alice@acme.com");
    }

    [Fact]
    public void ResolveManually_ShouldSetResolvedAtUtc()
    {
        var item = CreateItem();
        var before = DateTimeOffset.UtcNow;

        item.ResolveManually("user", "notes");

        item.ResolvedAtUtc.Should().NotBeNull();
        item.ResolvedAtUtc!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void InitialStatus_ShouldBeUnmatched()
    {
        var item = CreateItem();

        item.Status.Should().Be(MatchStatus.Unmatched);
    }

    [Fact]
    public void CreatedAtUtc_ShouldBeSetOnCreation()
    {
        var before = DateTimeOffset.UtcNow;
        var item = CreateItem();
        var after = DateTimeOffset.UtcNow;

        item.CreatedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
