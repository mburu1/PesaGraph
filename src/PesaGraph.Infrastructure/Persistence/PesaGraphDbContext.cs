using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PesaGraph.Shared.Domain;
using PesaGraph.Shared.Tenancy;

namespace PesaGraph.Infrastructure.Persistence;

public class PesaGraphDbContext : DbContext
{
    private readonly ICurrentTenantContext? _tenantContext;

    public PesaGraphDbContext(
        DbContextOptions<PesaGraphDbContext> options,
        ICurrentTenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PesaGraphDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically assign tenant ID to newly created tenant-scoped entities
        if (_tenantContext?.TenantId is not null)
        {
            var tenantId = _tenantContext.TenantId.Value;
            foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = tenantId;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
