namespace PesaGraph.Shared.Tenancy;

public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
