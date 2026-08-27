using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Reconciliation.Repositories;
using PesaGraph.Reconciliation.Services;

namespace PesaGraph.Reconciliation.DependencyInjection;

public static class ReconciliationServiceCollectionExtensions
{
    public static IServiceCollection AddReconciliationModule(this IServiceCollection services)
    {
        services.AddSingleton<IReconciliationRepository, InMemoryReconciliationRepository>();
        services.AddScoped<IReconciliationService, ReconciliationService>();

        return services;
    }
}
