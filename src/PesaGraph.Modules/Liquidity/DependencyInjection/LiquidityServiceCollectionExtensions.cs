using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Liquidity.Services;

namespace PesaGraph.Liquidity.DependencyInjection;

public static class LiquidityServiceCollectionExtensions
{
    public static IServiceCollection AddLiquidityModule(this IServiceCollection services)
    {
        services.AddScoped<ILiquidityService, LiquidityService>();
        return services;
    }
}
