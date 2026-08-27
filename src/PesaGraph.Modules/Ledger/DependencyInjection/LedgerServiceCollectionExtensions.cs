using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Ledger.Repositories;
using PesaGraph.Ledger.Services;

namespace PesaGraph.Ledger.DependencyInjection;

public static class LedgerServiceCollectionExtensions
{
    public static IServiceCollection AddLedgerModule(this IServiceCollection services)
    {
        services.AddSingleton<ILedgerRepository, InMemoryLedgerRepository>();
        services.AddScoped<ILedgerService, LedgerService>();

        return services;
    }
}
