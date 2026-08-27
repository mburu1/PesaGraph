using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Reconciliation.DependencyInjection;

public static class ReconciliationServiceCollectionExtensions
{
    public static IServiceCollection AddReconciliationModule(this IServiceCollection services)
    {
        return services;
    }
}
