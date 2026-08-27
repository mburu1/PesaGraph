using Microsoft.Extensions.DependencyInjection;

namespace PesaGraph.Tenancy.DependencyInjection;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        return services;
    }
}
