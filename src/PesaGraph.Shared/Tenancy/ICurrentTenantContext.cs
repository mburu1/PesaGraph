namespace PesaGraph.Shared.Tenancy;

public interface ICurrentTenantContext
{
    Guid? TenantId { get; }
    string? TenantCode { get; }
    bool HasTenant => TenantId.HasValue;
    void SetTenant(Guid tenantId, string? tenantCode = null);
}

public class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantCode { get; private set; }

    public void SetTenant(Guid tenantId, string? tenantCode = null)
    {
        TenantId = tenantId;
        TenantCode = tenantCode;
    }
}
