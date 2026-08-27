using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Audit.DependencyInjection;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        return services;
    }
}
