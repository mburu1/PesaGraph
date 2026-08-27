using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Liquidity.DependencyInjection;

public static class LiquidityServiceCollectionExtensions
{
    public static IServiceCollection AddLiquidityModule(this IServiceCollection services)
    {
        return services;
    }
}
