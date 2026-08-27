using Microsoft.Extensions.DependencyInjection;
using PesaGraph.Shared.Tenancy;
using PesaGraph.Shared.Time;

namespace PesaGraph.Shared.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();

        return services;
    }
}
