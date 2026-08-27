using System;
using System.Linq;
using FluentAssertions;
using PesaGraph.Shared.Domain;
using Xunit;

namespace PesaGraph.UnitTests.Shared;

// Concrete test double for AggregateRoot
file sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id) : base(id) { }

    public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

file sealed class OrderPlacedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_ShouldAddEventToDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var @event = new OrderPlacedEvent();

        aggregate.Raise(@event);

        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(@event);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleTimes_ShouldAccumulateEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.Raise(new OrderPlacedEvent());
        aggregate.Raise(new OrderPlacedEvent());

        aggregate.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyEventCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Raise(new OrderPlacedEvent());

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldBeReadOnly()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DomainEvents.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyCollection<IDomainEvent>>();
    }
}

public class EntityEqualityTests
{
    file sealed class IntEntity : Entity<int>
    {
        public IntEntity(int id) : base(id) { }
    }

    [Fact]
    public void Entities_WithSameId_ShouldBeEqual()
    {
        var id = 42;
        var e1 = new IntEntity(id);
        var e2 = new IntEntity(id);

        e1.Equals(e2).Should().BeTrue();
        (e1 == e2).Should().BeTrue();
    }

    [Fact]
    public void Entities_WithDifferentIds_ShouldNotBeEqual()
    {
        var e1 = new IntEntity(1);
        var e2 = new IntEntity(2);

        e1.Equals(e2).Should().BeFalse();
        (e1 != e2).Should().BeTrue();
    }

    [Fact]
    public void SameReference_ShouldBeEqual()
    {
        var e = new IntEntity(99);

        e.Equals(e).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameId_ShouldBeEqual()
    {
        var e1 = new IntEntity(7);
        var e2 = new IntEntity(7);

        e1.GetHashCode().Should().Be(e2.GetHashCode());
    }
}
