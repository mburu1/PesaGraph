using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Ledger.DependencyInjection;

public static class LedgerServiceCollectionExtensions
{
    public static IServiceCollection AddLedgerModule(this IServiceCollection services)
    {
        return services;
    }
}
