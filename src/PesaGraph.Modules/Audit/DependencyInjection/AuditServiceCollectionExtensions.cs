using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Audit.Repositories;
using PesaGraph.Audit.Services;

namespace PesaGraph.Audit.DependencyInjection;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
