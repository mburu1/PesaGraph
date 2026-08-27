using System;
using FluentAssertions;
using PesaGraph.Ingestion.Domain;
using PesaGraph.Shared.Enums;
using Xunit;

namespace PesaGraph.UnitTests.Ingestion;

public class RawWebhookEventTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static RawWebhookEvent CreateEvent(
        PaymentRail rail = PaymentRail.Mpesa,
        string eventType = "C2B_PAYMENT",
        string externalRef = "REF-001",
        string rawJson = "{\"amount\":1000}",
        string? headerJson = null,
        string idempotencyKey = "idem-key-001")
    {
        return RawWebhookEvent.Create(TenantId, rail, eventType, externalRef, rawJson, headerJson, idempotencyKey);
    }

    [Fact]
    public void Create_ShouldReturnEventWithExpectedProperties()
    {
        var @event = CreateEvent();

        @event.Should().NotBeNull();
        @event.Id.Should().NotBe(Guid.Empty);
        @event.TenantId.Should().Be(TenantId);
        @event.Rail.Should().Be(PaymentRail.Mpesa);
        @event.EventType.Should().Be("C2B_PAYMENT");
        @event.ExternalReference.Should().Be("REF-001");
        @event.RawJson.Should().Be("{\"amount\":1000}");
        @event.Status.Should().Be(IngestionStatus.Received);
        @event.ProcessedAtUtc.Should().BeNull();
        @event.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithHeaderJson_ShouldSetHeader()
    {
        var @event = CreateEvent(headerJson: "{\"x-api-key\":\"abc\"}");

        @event.HeaderJson.Should().Be("{\"x-api-key\":\"abc\"}");
    }

    [Fact]
    public void Create_WithoutHeaderJson_ShouldHaveNullHeader()
    {
        var @event = CreateEvent();

        @event.HeaderJson.Should().BeNull();
    }

    [Fact]
    public void MarkNormalised_ShouldUpdateStatusAndSetProcessedTime()
    {
        var @event = CreateEvent();

        @event.MarkNormalised();

        @event.Status.Should().Be(IngestionStatus.Normalised);
        @event.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkDuplicate_ShouldUpdateStatusAndSetProcessedTime()
    {
        var @event = CreateEvent();

        @event.MarkDuplicate();

        @event.Status.Should().Be(IngestionStatus.Duplicate);
        @event.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldUpdateStatusAndSetFailureReason()
    {
        var @event = CreateEvent();
        const string reason = "Invalid payload schema.";

        @event.MarkFailed(reason);

        @event.Status.Should().Be(IngestionStatus.Failed);
        @event.FailureReason.Should().Be(reason);
        @event.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldSetReceivedAtUtcToNow()
    {
        var before = DateTimeOffset.UtcNow;
        var @event = CreateEvent();
        var after = DateTimeOffset.UtcNow;

        @event.ReceivedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_ShouldAssignUniqueIds()
    {
        var event1 = CreateEvent();
        var event2 = CreateEvent();

        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void InitialStatus_ShouldBeReceived()
    {
        var @event = CreateEvent();

        @event.Status.Should().Be(IngestionStatus.Received);
    }

    [Fact]
    public void IdempotencyKey_ShouldBeStoredAsProvided()
    {
        var @event = CreateEvent(idempotencyKey: "unique-key-xyz");

        @event.IdempotencyKey.Should().Be("unique-key-xyz");
    }
}
