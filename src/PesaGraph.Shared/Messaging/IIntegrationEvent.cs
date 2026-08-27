namespace PesaGraph.Shared.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    Guid? TenantId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid? TenantId { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
